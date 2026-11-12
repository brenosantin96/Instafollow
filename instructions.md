# InstaFollow — instruções para o Claude

Ferramenta pessoal do Breno para achar **quem ele segue no Instagram e não segue de volta** e deixar de seguir
rápido, à mão. Roda no **PC** (uma página HTML) e no **celular Android** (app com a mesma página). Leia este arquivo
inteiro (e o `CLAUDE.md` do repositório) antes de mudar qualquer coisa. O Breno vai pedir mais alterações no futuro.

- Responder ao Breno em português do Brasil.
- Contas usadas: `@darkoriaot` (marca do servidor Darkoria, comercial e importante) e `@breno.santin` (pessoal).
- Criada em 12/11/2026, numa conversa do projeto Darkoria, mas **não faz parte do Darkoria**: não vai para o
  repositório `C:\Users\breno\dev\Darkoria` nem segue o CLAUDE.md de lá.

## Onde fica cada coisa

| Lugar | O que tem |
|---|---|
| `C:\Users\breno\source\repos\InstaFollow` | **Repositório Git, fonte de tudo.** GitHub `brenosantin96/Instafollow` (**público**), branch `main`. Tem `web/InstaFollow.html` (a ferramenta), `InstaFollow.Android/` (o app), `InstaFollow.slnx` (abrir no Visual Studio), `README.md`, `CLAUDE.md` (detalhes técnicos) e este `instructions.md`. |
| `C:\Users\breno\Desktop\Breno\InstaFollow` | Pasta de uso no PC, **fora do Git**: cópia do `InstaFollow.html`, cópia deste `instructions.md`, as exportações `.zip` reais das duas contas e o `InstaFollow-1.0.apk` gerado. |

- **Este arquivo existe nos dois lugares, com o mesmo conteúdo.** O do repositório vale; depois de mudar, copiar para a
  pasta do Desktop. O mesmo vale para o `InstaFollow.html`: editar `web/InstaFollow.html` no repositório e copiar
  para o Desktop.
- O Breno pediu (12/11/2026) que este arquivo vá também para o GitHub, mesmo o repositório sendo público.
- As exportações do Instagram (`.zip`) e os backups (`instafollow-backup-*.json`) são dados pessoais: **nunca** vão
  para o repositório (o `.gitignore` bloqueia) nem para nenhum serviço externo.
- Ao commitar: conferir `git status` antes; commit e push direto na `main` (o Breno pediu assim).

## Como a ferramenta funciona

1. **Dados:** a exportação oficial do Instagram (Central de Contas → Suas informações e permissões → Exportar suas
   informações → "Seguidores e seguindo", formato **JSON**, período **"desde o início" / "cualquier fecha"**).
   - **Cuidado com o período:** uma exportação só do último ano faz quem segue há mais tempo parecer "não segue de
     volta". Já aconteceu no primeiro pedido do Breno (12/11/2026); ele refez.
   - O `.zip` se chama `instagram-<usuario>-<AAAA-MM-DD>-<id>.zip`; a ferramenta tira daí a conta e a data.
     Sem esse padrão (ou com os `.json` soltos), ela pergunta a conta.
   - Arquivos lidos, em `connections/followers_and_following/`:
     - `followers_1.json` (e `followers_2.json`... se houver): lista de
       `{ string_list_data: [{ href, value, timestamp }] }`;
     - `following.json`: `{ relationships_following: [{ title, string_list_data: [{ href, value?, timestamp }] }] }`.
       Nas versões mais novas o usuário está em `title` e o `value` não vem; a ferramenta tenta `value`, `title` e o
       `href` (`instagram.com/_u/<usuario>`), nessa ordem. Usuários são normalizados em minúsculas.
   - `timestamp` = quando a relação começou (unix, segundos). Daí vem "você segue há X semanas".
2. **Leitura do .zip:** feita à mão no JS (diretório central do ZIP + `DecompressionStream('deflate-raw')`).
   Não há ZIP64 (as exportações são pequenas).
3. **Listas:** "não seguem de volta" = `following - followers`. A fila tira os marcados (feitos/pulados), aplica o
   filtro "sigo há pelo menos N semanas" (padrão 2) e ordena do seguido há mais tempo para o mais recente.
   Abas: Fila, Deixei de seguir, Pulados, "Seguem você e você não segue".
4. **Fluxo de deixar de seguir (decisão do Breno, 12/11/2026):** abrir perfil → no Instagram "Seguindo" → "Deixar de
   seguir" → na ferramenta "Deixei de seguir → Próximo", que marca e já abre o próximo. Pular (manter), Desfazer.
   - **PC:** o perfil abre numa **janela nomeada** (`window.open(url, 'instafollow_instagram')`, metade direita da
     tela), reaproveitada. Atalhos: Espaço = deixei de seguir e próximo, P = pular, A = abrir perfil, Z = desfazer.
   - **Celular:** o perfil abre no **app do Instagram**; o Breno deixa de seguir e volta (botão/gesto de voltar).
5. **Progresso:** `localStorage` (chaves `instafollow:v1:accounts`, `instafollow:v1:acc:<conta>`,
   `instafollow:v1:settings`). Por conta: `following`, `followers` (`{usuario: timestamp}`), `done` e `skipped`
   (`{usuario: quando marcou}`), `exportedAt`, `importedAt`. Backup/Restaurar salvam e carregam tudo num `.json`; o
   formato é o mesmo no PC e no celular, então dá para levar o progresso de um para o outro. PC e celular **não se
   sincronizam sozinhos**: cada um tem o seu progresso.
6. **Exportação nova da mesma conta:** troca as listas e mantém feitos/pulados. Quem estava como "deixei de seguir"
   mas ainda aparece como seguido numa exportação **feita depois** da marcação volta para a fila.
7. **Contador diário:** quantos foram marcados hoje, com aviso a partir de um limite (padrão 150/dia).

## O app Android (12/11/2026)

- **.NET 10 para Android puro** (`net10.0-android`, sem MAUI: a máquina tem só a carga de trabalho `android`; a
  `maui-android` não está instalada). Uma Activity com um WebView que mostra a mesma `InstaFollow.html`, embutida no
  APK e servida offline. Detalhes técnicos (ponte página↔app, origem fixa do `localStorage`, por que não usar
  `[Export]`) no `CLAUDE.md` do repositório.
- O que o app faz além da página: abre o perfil no app do Instagram, escolhe o `.zip`/backup pelos arquivos do
  celular, salva o backup onde o Breno escolher. Nenhuma requisição de rede sai da página (o app bloqueia).
- Pacote `io.github.brenosantin96.instafollow`, Android 8+ (minSdk 26), ARM 64 e 32 bits, APK de ~7,7 MB.
- Ferramentas na máquina (vieram com o Visual Studio): SDK Android em `C:\Program Files (x86)\Android\android-sdk`
  (platform 36, build-tools 36.0.0, `adb` em `platform-tools`), JDK 21 em `C:\Program Files\Android\openjdk`,
  .NET SDK 10.0.401. Os projetos do Breno ficam em `C:\Users\breno\source\repos` (ele já tem apps MAUI lá).
- **Gerar o APK:** `dotnet build InstaFollow.Android -c Release` →
  `InstaFollow.Android\bin\Release\net10.0-android\io.github.brenosantin96.instafollow-Signed.apk`. Renomear para
  `InstaFollow-<versão>.apk` e copiar para a pasta do Desktop.
- **A cada versão nova:** subir `ApplicationVersion` (inteiro: 1, 2, 3...) e `ApplicationDisplayVersion` (1.0, 1.1...)
  no `.csproj`, senão o Android não instala por cima.
- **Assinatura:** com a chave de depuração **deste PC** (`%LOCALAPPDATA%\Xamarin\Mono for Android\debug.keystore`).
  Atualização só instala por cima (mantendo o progresso) se vier assinada com a mesma chave. Formatou o PC / perdeu a
  chave → desinstalar o app (perde o progresso: fazer Backup antes).
- **Desinstalar o app apaga o progresso.** Lembrar o Breno do Backup antes de qualquer reinstalação.

### Instalar no celular (o que aconteceu em 12/11/2026)

- O APK foi passado para o celular e instalado à mão. "Permitir fontes desconhecidas" **não bastou**: o **Google Play
  Protect bloqueou** a instalação. Resolveu desligando temporariamente o Play Protect (Play Store → foto → Play
  Protect → engrenagem → desligar "Verificar apps com o Play Protect"), instalando, e ligando de novo. Funcionou.
- Se um dia isso não bastar (versões novas do Android bloqueiam apps de desenvolvedor não verificado): instalar pelo
  cabo USB, que continua liberado. No celular: Configurações → Sobre o telefone → 7 toques em "Número da versão" →
  Opções do desenvolvedor → "Depuração USB"; ligar no PC e aceitar. Daí o Claude roda
  `adb install -r <apk>` (o `-r` mantém o progresso). Com o cabo também dá para ver erros do app (`adb logcat`) e, em
  build Debug, inspecionar a página em `chrome://inspect`.
- Release no GitHub com o APK: **não foi publicada** (o sistema de permissões do Claude bloqueou o `gh release
  create`). Se o Breno quiser baixar pelo celular, ele mesmo roda no repositório:
  `gh release create v<versão> "<caminho do apk>" --title "InstaFollow <versão>"`.
- O app **ainda não foi exercitado a fundo no celular** (só instalado). Pontos que podem variar de aparelho para
  aparelho: abrir o Instagram, escolher o `.zip`, salvar o backup, áreas sob as barras do sistema.

## Decisão importante: o "deixar de seguir" é manual de propósito

- Os Termos do Instagram proíbem ações automatizadas. Deixar de seguir em massa é o padrão que ele trata como spam:
  bloqueio temporário de ações, e em casos repetidos restrição da conta. A `@darkoriaot` é a conta da marca de um
  servidor comercial: perder ou restringir essa conta custa caro.
- Os limites não são públicos; relatos falam em ~150-200 deixar-de-seguir por dia, menos em contas novas ou pequenas.
  Recomendação dada ao Breno: espalhar em alguns dias, ~100-150 por dia.
- Por isso nem a página nem o app **clicam no Instagram** nem pedem senha: só leem a exportação oficial e abrem os
  perfis. Quem age é o Breno. A ferramenta não sabe se o deixar de seguir aconteceu: confia no "Próximo".

## Ideias para "mais automatizado" (o Breno quer evoluir) e o risco de cada uma

Explique o risco ao Breno antes de fazer qualquer uma; a decisão é dele, e para a `@darkoriaot` vale perguntar se
ele quer arriscar a conta da marca.

| Ideia | Ganho | Risco |
|---|---|---|
| Atalho global/automação de teclado só para trocar de janela (sem clicar no Instagram) | pouco | nenhum |
| Mostrar na lista a foto/nome do perfil | conforto | baixo, mas exigiria buscar dados no Instagram (scraping); evitar |
| Extensão do navegador (PC) que, na página do perfil aberta pela ferramenta, destaca o botão "Seguindo" ou confirma o diálogo com uma tecla | 1 clique a menos | médio: é automação de clique no site; manter sempre um humano por ação e ritmo humano |
| Serviço de Acessibilidade no Android que toca em "Seguindo"/"Deixar de seguir" no app do Instagram | toques a menos | médio a alto: automação dentro do app; o Play Protect e o Instagram desconfiam desse tipo de app |
| Script/extensão que deixa de seguir sozinho com pausas aleatórias | máximo | **alto**: viola os Termos; bloqueio de ações ou da conta. Não recomendado, sobretudo na `@darkoriaot` |
| Bibliotecas não oficiais (ex. `instagrapi`) ou apps da loja | máximo | **alto**: pedem a senha, disparam verificação de segurança, risco de roubo/bloqueio da conta |
| API oficial (Instagram Graph API) | — | não serve: não tem endpoint para seguir/deixar de seguir |

## Como testar mudanças

- Página: editar `web/InstaFollow.html` e recarregar no navegador. Sintaxe: extrair o `<script>` para um `.js` e
  `node --check`. Modo app no PC: Edge headless com user agent contendo `InstaFollowAndroid/1`
  (`msedge --headless=new --user-agent=... --dump-dom <arquivo>`) deve dar `<html class="in-app">`.
- Leitura do ZIP sem dados reais: gerar um `.zip` de exemplo (Python `zipfile`) e rodar no Node 22 o trecho entre
  `// ---------- ZIP reading` e `async function importFiles`.
- App: `dotnet build InstaFollow.Android -c Release` e `-c Debug`, ambos com 0 avisos e 0 erros. Conferir o APK com
  `aapt2 dump badging` e `apksigner verify --print-certs` (build-tools 36.0.0).
- Nunca usar a exportação real do Breno em ferramentas ou serviços online.

## Histórico

- **12/11/2026:** primeira versão da página (leitura do .zip/.json, fila com filtro por semanas, janela do Instagram
  reaproveitada, Próximo/Pular/Desfazer com atalhos, abas, contador diário, backup/restauração, várias contas).
  Testada com exportação de exemplo.
- **12/11/2026:** o Breno testou com as exportações reais das duas contas (`@breno.santin` e `@darkoriaot`, período
  completo): funcionou.
- **12/11/2026:** pediu um app para o celular Android. Feito: repositório `C:\Users\breno\source\repos\InstaFollow`
  (GitHub `brenosantin96/Instafollow`, primeiro commit na `main`), app .NET 10 para Android com a mesma página, página
  com layout de celular (botões grandes, sem dicas de teclado no toque, textos de ajuda do app), `README.md` e
  `CLAUDE.md`. APK `InstaFollow-1.0.apk` (versão 1.0, versionCode 1) na pasta do Desktop.
- **12/11/2026:** instalação no celular: o Play Protect bloqueou; resolvido desligando-o temporariamente. App
  instalado e abrindo. Este `instructions.md` passou a ir também para o repositório, a pedido do Breno.
