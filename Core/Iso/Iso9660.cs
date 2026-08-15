using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FerramentaAFS
{
    public sealed class IsoFileEntry
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public uint Lba { get; set; }
        public uint Size { get; set; }
        public bool IsDirectory { get; set; }
        public long DirectoryRecordOffset { get; set; }
        public long DataOffset => (long)Lba * 2048L;
    }

    public static class Iso9660Reader
    {
        private const int SectorSize = 2048;

        public static List<IsoFileEntry> ReadAllFiles(string isoPath)
        {
            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] pvd = new byte[SectorSize];
            fs.Position = 16L * SectorSize;
            ReadExactly(fs, pvd, 0, pvd.Length);

            if (pvd[0] != 1 || Encoding.ASCII.GetString(pvd, 1, 5) != "CD001")
                throw new InvalidDataException("A imagem não possui um Primary Volume Descriptor ISO9660 válido.");

            int rootLength = pvd[156];
            if (rootLength < 34)
                throw new InvalidDataException("Registro do diretório raiz ISO9660 inválido.");

            IsoFileEntry root = ParseRecord(pvd, 156, 16L * SectorSize + 156, string.Empty);
            root.Name = string.Empty;
            root.FullPath = string.Empty;

            List<IsoFileEntry> result = new List<IsoFileEntry>();
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ReadDirectory(fs, root, result, visited);
            return result;
        }

        private static void ReadDirectory(FileStream fs, IsoFileEntry dir, List<IsoFileEntry> result, HashSet<string> visited)
        {
            string key = $"{dir.Lba}:{dir.Size}";
            if (!visited.Add(key))
                return;

            long start = dir.DataOffset;
            long end = start + dir.Size;
            long pos = start;

            while (pos < end)
            {
                fs.Position = pos;
                int length = fs.ReadByte();

                if (length < 0)
                    break;

                if (length == 0)
                {
                    pos = ((pos / SectorSize) + 1) * SectorSize;
                    continue;
                }

                if (pos + length > end)
                    break;

                byte[] record = new byte[length];
                record[0] = (byte)length;
                ReadExactly(fs, record, 1, length - 1);

                IsoFileEntry entry = ParseRecord(record, 0, pos, dir.FullPath);

                pos += length;

                if (entry.Name == "." || entry.Name == "..")
                    continue;

                result.Add(entry);

                if (entry.IsDirectory)
                    ReadDirectory(fs, entry, result, visited);
            }
        }

        private static IsoFileEntry ParseRecord(byte[] data, int offset, long recordOffset, string parent)
        {
            int length = data[offset];
            if (length < 34)
                throw new InvalidDataException("Registro ISO9660 curto demais.");

            uint lba = BitConverter.ToUInt32(data, offset + 2);
            uint size = BitConverter.ToUInt32(data, offset + 10);
            byte flags = data[offset + 25];
            int nameLength = data[offset + 32];

            string name;
            if (nameLength == 1 && data[offset + 33] == 0)
                name = ".";
            else if (nameLength == 1 && data[offset + 33] == 1)
                name = "..";
            else
            {
                name = Encoding.ASCII.GetString(data, offset + 33, nameLength);
                int semicolon = name.IndexOf(';');
                if (semicolon >= 0)
                    name = name.Substring(0, semicolon);
            }

            string full = string.IsNullOrEmpty(parent) ? name : parent + "/" + name;

            return new IsoFileEntry
            {
                Name = name,
                FullPath = full,
                Lba = lba,
                Size = size,
                IsDirectory = (flags & 0x02) != 0,
                DirectoryRecordOffset = recordOffset
            };
        }

        public static void UpdateFileSize(string isoPath, IsoFileEntry entry, uint newSize)
        {
            using FileStream fs = new FileStream(isoPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII, leaveOpen: true);

            fs.Position = entry.DirectoryRecordOffset + 10;
            bw.Write(newSize);

            fs.Position = entry.DirectoryRecordOffset + 14;
            WriteUInt32BigEndian(bw, newSize);

            fs.Flush(true);
            entry.Size = newSize;
        }

        private static void WriteUInt32BigEndian(BinaryWriter bw, uint value)
        {
            bw.Write((byte)(value >> 24));
            bw.Write((byte)(value >> 16));
            bw.Write((byte)(value >> 8));
            bw.Write((byte)value);
        }

        private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int read = stream.Read(buffer, offset, count);
                if (read <= 0)
                    throw new EndOfStreamException();
                offset += read;
                count -= read;
            }
        }
    }

    public sealed class BoundedFileStream : Stream
    {
        private readonly FileStream _file;
        private readonly long _baseOffset;
        private readonly long _length;
        private long _position;

        public BoundedFileStream(string path, long baseOffset, long length, FileAccess access, FileShare share)
        {
            _file = new FileStream(path, FileMode.Open, access, share);
            _baseOffset = baseOffset;
            _length = length;

            if (baseOffset < 0 || length < 0 || baseOffset + length > _file.Length)
                throw new ArgumentOutOfRangeException(nameof(length), "A região solicitada ultrapassa o arquivo físico.");
        }

        public override bool CanRead => _file.CanRead;
        public override bool CanSeek => true;
        public override bool CanWrite => _file.CanWrite;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > _length)
                    throw new IOException("Posição fora da região AFS.");
                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
                return 0;

            count = (int)Math.Min(count, _length - _position);
            _file.Position = _baseOffset + _position;
            int read = _file.Read(buffer, offset, count);
            _position += read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_position + count > _length)
                throw new IOException("A escrita ultrapassaria a área reservada ao AFS dentro da ISO.");

            _file.Position = _baseOffset + _position;
            _file.Write(buffer, offset, count);
            _position += count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long next = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            Position = next;
            return _position;
        }

        public override void Flush() => _file.Flush();
        public void Flush(bool flushToDisk) => _file.Flush(flushToDisk);
        public override void SetLength(long value) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) _file.Dispose();
            base.Dispose(disposing);
        }
    }
}
