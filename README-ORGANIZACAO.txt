ALTERAÇÕES
- Projeto reorganizado em Core/Iso, UI, UI/Theming, Localization e Settings.
- Form1.Import.resx duplicado removido; somente UI/Form1.resx permanece.
- Configurações > Idioma: Português / Inglês, persistente.
- Configurações > Mostrar mensagens de sucesso: permite desligar feedbacks positivos de extração/importação.
- Extrair Todos e Importar Todos agora exibem BatchProgressForm com barra, arquivo atual e Cancelar; a janela é atualizada durante o lote.
- Textos principais da interface e menus mudam em tempo de execução. Mensagens técnicas antigas ainda podem aparecer em português; agora estão isoladas para migração gradual ao Localization/Loc.cs.
- Preferências ficam em %LocalAppData%\FerramentaAFS\settings.json.
