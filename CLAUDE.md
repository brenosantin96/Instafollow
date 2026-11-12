# InstaFollow — instruções para o Claude

Ferramenta pessoal para achar **quem o dono segue no Instagram e não segue de volta** e deixar de seguir rápido, à mão.
Responder em português do Brasil. **Ler também o `instructions.md` (raiz do repositório)**: contas usadas,
decisões do dono, como o app foi instalado no celular e o histórico. Repositório **público**: nunca commitar
exportações, backups nem chaves (o `.gitignore` já bloqueia `*.zip`, `instafollow-backup-*.json`,
`followers_*.json`, `following.json`, `*.keystore`).

## Estrutura

| Caminho | O que é |
|---|---|
| `web/InstaFollow.html` | **A ferramenta inteira**: um HTML com CSS e JS embutidos, sem bibliotecas e sem rede. No PC abre com duplo clique (Chrome/Edge). É a única fonte da lógica. |
| `InstaFollow.Android/` | App .NET 10 para Android (`net10.0-android`, sem MAUI; só a carga de trabalho `android`). Uma Activity com um WebView que mostra a página. |
| `InstaFollow.slnx` | Solução para o Visual Studio. |
| `instructions.md` | Contexto do dono e histórico. Cópia idêntica em `C:\Users\breno\Desktop\Breno\InstaFollow`: mudar aqui e copiar para lá. |

## Como a página funciona

1. **Dados:** exportação oficial do Instagram ("Seguidores e seguindo", **JSON**, período **desde o início**: com só
   o último ano, quem segue há mais tempo parece "não segue de volta").
   - `.zip` chamado `instagram-<usuario>-<AAAA-MM-DD>-<id>.zip`: daí saem conta e data. Sem esse padrão, a página
     pergunta a conta (`prompt`).
   - Em `connections/followers_and_following/`: `followers_1.json` (`followers_2.json`...) = lista de
     `{ string_list_data: [{ href, value, timestamp }] }`; `following.json` = `{ relationships_following: [{ title,
     string_list_data: [{ href, value?, timestamp }] }] }`. Usuário: `value`, senão `title`, senão o `href`
     (`instagram.com/_u/<usuario>`), em minúsculas. `timestamp` = início da relação (unix, s).
2. **ZIP** lido à mão (diretório central + `DecompressionStream('deflate-raw')`), sem ZIP64.
3. **Fila** = `following - followers`, sem os marcados (feitos/pulados), filtro "sigo há pelo menos N semanas"
   (padrão 2), do seguido há mais tempo para o mais recente.
4. **Fluxo:** abrir perfil → no Instagram, "Seguindo" → "Deixar de seguir" → na ferramenta, "Deixei de seguir →
   Próximo", que marca e já abre o próximo. Pular (manter), Desfazer. No PC: atalhos Espaço/P/A/Z e uma janela nomeada
   (`instafollow_instagram`) reaproveitada.
5. **Progresso** no `localStorage` (`instafollow:v1:accounts`, `instafollow:v1:acc:<conta>`,
   `instafollow:v1:settings`). Backup/Restaurar em `.json` (`{ tool: 'InstaFollow', version: 1, settings, accounts }`),
   o mesmo formato no PC e no app, então dá para levar o progresso de um para o outro.
6. **Exportação nova da mesma conta:** troca as listas, mantém feitos/pulados; quem estava como "deixei de seguir" e
   ainda aparece seguido numa exportação **posterior** à marcação volta para a fila.
7. **Contador diário** com aviso (padrão 150/dia).

## O app Android

- O WebView carrega `https://appassets.androidplatform.net/index.html`; `ShouldInterceptRequest` serve o asset
  `index.html` (o `web/InstaFollow.html`, ligado no `.csproj` como `AndroidAsset`) e **responde 403 a qualquer outra
  requisição**: a página roda sem rede. A origem https fixa mantém o `localStorage` entre atualizações: **não mudar
  o host**, senão o progresso some.
- A página detecta o app por `InstaFollowAndroid/` no user agent (classe `in-app` no `<html>`, variável `IN_APP`).
- **Ponte página → app:** `prompt('instafollow:<comando>', JSON.stringify([args]))`, capturado em `OnJsPrompt`
  (outros `prompt` passam como diálogo normal). Não usar `AddJavascriptInterface`/`[Export]`: precisam de reflexão e
  quebram com o trimming do Release (aviso IL2026). Comandos: `openProfile [usuario]` (Intent para
  `https://www.instagram.com/<usuario>/` com o pacote `com.instagram.android`; sem o app, o navegador) e
  `saveFile [nome, texto]` (`ACTION_CREATE_DOCUMENT`; resposta em `window.instafollowSaved(ok, mensagem)`).
- `<input type="file">` → `OnShowFileChooser` → `ACTION_GET_CONTENT` com `*/*` (os celulares dão tipos variados a
  `.zip`/`.json`). Cancelado: devolver `null` ao callback, senão o input trava.
- Android 15+ desenha atrás das barras do sistema: o `FrameLayout` raiz recebe as insets como padding.
- Versão: subir `ApplicationVersion` (inteiro) e `ApplicationDisplayVersion` no `.csproj` a cada release, senão o
  Android não instala por cima.
- **Assinatura:** o Release é assinado com a chave de depuração desta máquina
  (`%LOCALAPPDATA%\Xamarin\Mono for Android\debug.keystore`). Uma atualização só instala por cima se vier assinada com
  a mesma chave. Se essa chave se perder (formatar o PC), o app tem de ser desinstalado (perde o progresso: fazer
  Backup antes). Se um dia precisar de chave própria: `keytool`, guardada fora do repositório.

## Decisão importante: o "deixar de seguir" é manual de propósito

- Os Termos do Instagram proíbem ações automatizadas; deixar de seguir em massa leva a bloqueio temporário e, se
  repetido, restrição da conta. Limites não públicos (~150-200/dia, menos em contas novas). Recomendação: 100-150/dia.
- Por isso nem a página nem o app clicam no Instagram nem pedem senha; só leem a exportação oficial e abrem perfis.
  O app também não sabe se o deixar de seguir aconteceu: confia no "Próximo".
- Ideias "mais automáticas" (extensão que clica, script, bibliotecas não oficiais como `instagrapi`, Accessibility
  Service no Android que toca nos botões): explicar o risco ao dono antes; a decisão é dele. A API oficial (Graph API)
  não tem endpoint de seguir/deixar de seguir.

## Como testar

- Página: editar `web/InstaFollow.html` e recarregar no navegador. Sintaxe: extrair o `<script>` para um `.js` e
  `node --check`. Leitura do ZIP: gerar um `.zip` de exemplo (Python `zipfile`) e rodar no Node 22 o trecho entre
  `// ---------- ZIP reading` e `async function importFiles`.
- App: `dotnet build InstaFollow.Android -c Release` (0 avisos esperado). APK em
  `InstaFollow.Android/bin/Release/net10.0-android/io.github.brenosantin96.instafollow-Signed.apk`. Conferir com
  `aapt2 dump badging` (build-tools em `C:\Program Files (x86)\Android\android-sdk\build-tools`). Com o celular por
  USB (depuração USB ligada): `adb install -r <apk>`; inspecionar a página no `chrome://inspect` só em Debug.
- Publicar: `gh release create v<versão> <apk renomeado para InstaFollow-<versão>.apk>`.
- Nunca usar a exportação real do dono em ferramentas ou serviços online.
