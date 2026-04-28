# Setup en Unity tras hacer pull

Si acabas de hacer `pull.bat` y hay codigo nuevo, sigue esta guia para
que todo funcione en la escena.

## 1. Abrir la escena correcta

`Assets/Scenes/EscenarioGeneral.unity` (la del Bloque 2).

## 2. Crear el GameObject `_Managers`

En la jerarquia (panel izquierdo de la escena):

1. Clic derecho en zona vacia -> **Create Empty**
2. Renombrar el objeto a `_Managers`
3. Posicion: cualquier sitio, da igual (ej: 0, 0, 0)

> Si ya existe un objeto vacio que agrupe managers, usa ese y salta este paso.

## 3. Anadir los 4 componentes nuevos a `_Managers`

Con `_Managers` seleccionado, en el panel **Inspector** (a la derecha):

Pulsa **Add Component** y escribe estos nombres uno por uno:

| Componente | Que hace |
|---|---|
| `MapaInfluencia` | Calcula la influencia de cada bando en el mapa cada 0.5s |
| `EstrategiaBando` | Modos Defensivo / Ofensivo / Guerra Total. Teclas 1-4 y T |
| `MinimapaInfluencia` | Heatmap rojo/azul en pantalla. Tecla M para ocultar |
| `HelpScreen` | Pantalla de ayuda con todas las teclas. F1 |

> Los **defaults** estan bien. No hace falta tocar ningun campo.

## 4. Comprobar que ya existen estos otros componentes en la escena

Si la escena ya funcionaba antes, deberian estar. Si no, hay que crearlos:

| GameObject | Componentes que debe tener | Notas |
|---|---|---|
| **GridSystem** (o similar) | `GridManager` + `Pathfinding` | Tienen que estar en el MISMO GameObject porque Pathfinding hace `GetComponent<GridManager>()` |
| **WayPoints** | `WayPoints` con todos los Transform asignados (spawnRojo, spawnAzul, hospitalRojo, hospitalAzul, baseRojo, baseAzul) | Sin esto, los NPCs no saben donde curarse ni donde esta la base enemiga |
| **Spawner** | `NPCRespawnSpawner` con prefabs de cada tipo asignados | |
| **Main Camera** | `MainCameraController` | Para mover camara con WASD |
| **CastilloRojo** y **CastilloAzul** | (cualquier modelo / cubo) | Asignados en `CastilloDerrumbado` |
| **GameController** | `CastilloDerrumbado` con CastilloRojo y CastilloAzul referenciados | Condicion de victoria |
| **SeleccionUnidad** | `SeleccionarUnidad` | Para seleccionar con click |

## 5. Comprobar que el prefab de NPC tiene todo lo necesario

Cada NPC (Caballero, Arquero, Lancero, Tanque, Explorador) debe tener:

- `NPCStats` con `tipoUnidad` correcto y `miBando` (Rojo o Azul)
- `PercepcionNPC` (busca Mapa/Estrategia/Waypoints sola en runtime)
- `estadoNPC` con un estado inicial (Vigilancia / Ataque / Defensa)
- `NPCPatrol` con waypointsRojo y waypointsAzul asignados (si patrulla)
- `AgentNPC` + steerings (Arrive, Face, etc.)
- `PathFollowing`
- Layer `Unidad` (o la que useis para detectarse entre NPCs)

> No hace falta tocar nada nuevo en el prefab. Las modificaciones funcionan
> con lo que ya teniais, leyendo los managers en runtime.

## 6. Pulsar Play y probar

| Tecla | Que pasa |
|---|---|
| **F1** | Abre la pantalla de ayuda con TODAS las teclas |
| **M** | Toggle minimapa de influencia |
| **I** | Toggle componente tactico del A* (ver diferencia en los caminos) |
| **1** / **2** | Bando Rojo Defensivo / Ofensivo |
| **3** / **4** | Bando Azul Defensivo / Ofensivo |
| **T** | Toggle GUERRA TOTAL (todos a por la victoria) |
| **G** | Toggle visualizacion del grid |
| **C** | Toggle gizmos de percepcion |
| **J** / **K** | Debug danio / curacion (a la unidad seleccionada) |
| **WASD** + **Shift** | Mover camara |

## Como saber si esta funcionando

- **Esquina superior derecha**: deberias ver "Rojo: Defensivo / Azul: Defensivo".
  Si pulsas T, cambia a "*** GUERRA TOTAL ***" en rojo.
- **Esquina inferior izquierda**: "F1: ayuda de teclas".
- **Pulsa M**: aparece un cuadro arriba a la izquierda con un mapa de
  manchas rojas y azules. Si los NPCs se mueven, las manchas se mueven.
- **Pulsa Play y deja correr**: en la consola veras logs tipo
  `Caballero (Caballero) -> Lancero (Lancero): FA=120 FD=80 dano=187`
  cada vez que un NPC ataca.
- **Critico**: cada ~100 ataques aparece `<color=orange>CRITICO</color>`
  en la consola con un dano enorme.

## Si algo NO funciona

| Sintoma | Causa probable | Solucion |
|---|---|---|
| Minimapa en gris | MapaInfluencia no encuentra GridManager | Anade GridManager a la escena o revisa la consola |
| NPCs se quedan parados sin enemigo | No hay WayPoints en la escena | Crea GameObject con WayPoints y asigna spawnRojo/Azul, baseRojo/Azul, hospitalRojo/Azul |
| Hospital no cura | Falta la layer "Unidad" | Edit -> Project Settings -> Tags and Layers, anade layer `Unidad` |
| "Object reference not set..." al hacer Play | A algun GameObject le falta un componente del paso 4 | Lee el error en la consola, te dice cual |
| El minimapa pinta cosas pero los NPCs no reaccionan | El A* no esta usando el mapa | Pulsa I para asegurarte de que el componente tactico esta ON |

## Si todo se rompe (ultimo recurso)

`reset-unity.bat` (cierra Unity primero). Borra solo la cache local,
no toca codigo ni assets. Despues abre Unity y deja que reimporte
(tarda varios minutos la primera vez).
