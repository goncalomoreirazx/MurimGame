# Heavenly Warrior

Jogo de ação 2D (Murim / Wuxia) feito em Unity. Por agora não há história: o objetivo é ter o movimento e o combate a funcionar e a serem divertidos.

## Arrancar o projeto Unity (só na primeira vez)

O repositório ainda não tem os ficheiros do projeto Unity (`ProjectSettings`, `Packages`), só os scripts e os sprites. Para criar o projeto:

1. No Unity Hub, cria um projeto novo com o template **Universal 2D** (Unity 6), numa pasta qualquer.
2. Fecha o Unity. Copia as pastas `Assets`, `Packages` e `ProjectSettings` desse projeto para a pasta deste repositório (se o Windows perguntar, junta a pasta `Assets` com a que já existe).
3. Abre a pasta do repositório no Unity Hub (**Add → Add project from disk**).
4. Faz commit e push de `Assets`, `Packages` e `ProjectSettings` (o `.gitignore` já deixa de fora `Library`, `Temp`, etc.).

## Testar o protótipo

1. Abre qualquer cena (a `SampleScene` do template serve) e carrega em **Play**.
2. O nível 1 (Floresta Mística ao Luar) é montado sozinho, como um nível de plataformas clássico: começas no portão vermelho à esquerda e tens de chegar ao portal azul no fim, à direita.
   - Pelo caminho há buracos, plataformas (uma delas móvel), pilares, orbes de Qi para apanhar e duas lanternas de checkpoint que acendem quando passas.
   - Cair num buraco tira 2 de vida e volta ao último checkpoint. Se morreres, R recomeça do último checkpoint.
   - Os inimigos estão espalhados: um boneco de treino no início, lobos, jiangshi e o golem a guardar o portal.
   - Ao chegar ao portal aparece o ecrã de vitória com o tempo, as orbes, os inimigos derrotados e as quedas. R joga outra vez desde o início.

| Ação | Teclado | Comando |
|---|---|---|
| Mover | A / D ou setas | Analógico esquerdo / cruz |
| Saltar (2x no ar) | Espaço, W ou seta cima | A |
| Atacar (combo de 3) | J ou clique esquerdo | X |
| Dash (invencível) | Shift esquerdo ou L | B ou RB |
| Recomeçar | R (depois de morrer ou de chegar ao portal) | Start |

Os inimigos patrulham, perseguem quando te veem e ficam avermelhados antes de atacar (é a altura de fugir ou dar dash).

| Inimigo | Vida | Como se comporta |
|---|---|---|
| Lobo | 4 | Rápido, investida curta. Cada golpe teu interrompe-o. |
| Jiangshi | 5 | Lento, garra de energia com mais alcance. |
| Golem | 14 | Muito lento e forte (2 de dano). Não é interrompido enquanto ataca: bate e afasta-te. |

Os valores de cada inimigo estão em `Assets/_Project/Scripts/Prototype/EnemyTypes.cs`.

## Onde mexer

- `Assets/_Project/Scripts/Player`: movimento (`PlayerController`), combo e hitboxes (`PlayerCombat`), animações (`PlayerAnimation`).
- `Assets/_Project/Scripts/Enemies/EnemyAI.cs`: comportamento do inimigo.
- `Assets/_Project/Scripts/Prototype/PrototypeArena.cs`: o nível de teste. O desenho do nível está todo no método `BuildLayout()` (chão, buracos, plataformas, orbes, checkpoints, inimigos). Desliga-se sozinho quando uma cena já tiver um `PlayerController`.
- `Assets/_Project/Scripts/Level`: plataformas móveis, checkpoints, orbes de Qi e o portal da meta.
- Todos os valores (velocidade, salto, dano, tempos dos golpes) aparecem no Inspector durante o Play: seleciona `Jogador` ou um inimigo na Hierarchy e ajusta. Atenção que as mudanças feitas em Play perdem-se ao sair.

## Sprites e cenário

- A folha original está em `ArtSource/espadachins_elementais.png`.
- `Tools/slice_espadachins.py` corta-a em frames individuais em `Assets/_Project/Resources/Characters/Blue` e `Red`, todos com o mesmo tamanho e os pés no mesmo sítio.
- Ao entrarem no Unity, o script `CharacterSpriteImporter` configura-os sozinho (Sprite, 100 px por unidade, pivot nos pés).
- Para trocar um frame de uma animação, edita a tabela `ANIMS` no script e volta a correr `python3 Tools/slice_espadachins.py`.
- Os nomes dos ficheiros seguem `<animação>_<nn>.png` (idle, run, jump, fall, dash, attack1..3, hurt, death, special). Uma personagem nova é só uma pasta nova em `Resources/Characters` com os mesmos nomes.

### Criaturas e fundo do primeiro nível

- Originais em `ArtSource/criaturas.png` e `ArtSource/floresta_mistica_luar.png`.
- `Tools/slice_level1.py` corta as criaturas (Wolf, Jiangshi, Golem) para `Resources/Characters` e divide o fundo em três partes em `Resources/Levels/Forest`: `backdrop` (céu, montanhas e floresta, com parallax), `ground` (caminho e muro de pedra, repetido ao longo do chão) e `ledge` (topo do muro, usado nas plataformas).
- O `CharacterSpriteImporter` também configura estas imagens ao entrarem no Unity.
