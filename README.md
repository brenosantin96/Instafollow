# InstaFollow

Acha quem você segue no Instagram e **não segue de volta**, e ajuda a deixar de seguir rápido, uma pessoa por vez.

- **Sem senha, sem login, sem internet:** lê só a exportação oficial do Instagram (o `.zip` de "Seguidores e seguindo").
  Nada sai do aparelho.
- **Quem deixa de seguir é você:** a ferramenta abre o perfil e você toca em "Seguindo" → "Deixar de seguir".
  Ela não clica no Instagram, porque automação de deixar de seguir viola os Termos e leva a bloqueio da conta.

Funciona em dois lugares, com a mesma página:

| Onde | Como |
|---|---|
| **Celular Android** | App `InstaFollow` (APK em [Releases](../../releases)). O perfil abre no app do Instagram. |
| **PC** | Abrir [`web/InstaFollow.html`](web/InstaFollow.html) no Chrome ou Edge. O perfil abre numa janela ao lado. |

## Instalar no celular

1. No celular, abra a página [Releases](../../releases) deste repositório e baixe o `InstaFollow-<versão>.apk`.
2. Abra o arquivo baixado. Na primeira vez o Android pede para **permitir instalar apps desta fonte**
   (Chrome ou "Arquivos"): permita e volte.
3. Se o Play Protect avisar que o app é desconhecido: **Mais detalhes → Instalar mesmo assim**
   (o app não está na Play Store, por isso o aviso).

Para atualizar, instale o APK novo por cima: o progresso continua. **Desinstalar apaga o progresso**: use o botão
Backup antes.

## Pegar a exportação do Instagram

Instagram → Central de Contas → Suas informações e permissões → **Exportar suas informações** → Criar exportação →
Exportar para o dispositivo → **Personalizar informações: só "Seguidores e seguindo"** → Período: **Desde o início** →
Formato: **JSON**. Quando ficar pronto (minutos ou horas), baixe o `.zip` e escolha no app em **Escolher arquivo**.

O período precisa ser "Desde o início": com só o último ano, quem te segue há mais tempo parece "não segue de volta".

## Usar

1. **Abrir perfil** → o perfil abre no Instagram → **Seguindo** → **Deixar de seguir** → voltar.
2. **Deixei de seguir → Próximo**: marca e já abre o próximo no Instagram.
3. **Pular (manter)** para quem você quer continuar seguindo. **Desfazer** volta o último.
4. Faça uns **100-150 por dia** no máximo: o Instagram bloqueia temporariamente quem deixa de seguir muita gente de
   uma vez. O app mostra o contador do dia.

## Compilar

Precisa do Visual Studio com a carga de trabalho **.NET Multi-platform App UI / Android** (ou o .NET 10 SDK com
`dotnet workload install android`).

- Visual Studio: abrir `InstaFollow.slnx`, escolher o celular (depuração USB ligada) e rodar.
- Linha de comando: `dotnet build InstaFollow.Android -c Release` gera
  `InstaFollow.Android/bin/Release/net10.0-android/io.github.brenosantin96.instafollow-Signed.apk`.

Estrutura:

| Caminho | O que é |
|---|---|
| `web/InstaFollow.html` | A ferramenta inteira (HTML + CSS + JS, sem bibliotecas). Usada no PC e embutida no app. |
| `InstaFollow.Android/` | App .NET para Android: uma tela com a página, mais a ponte para abrir o Instagram, escolher arquivos e salvar o backup. |
