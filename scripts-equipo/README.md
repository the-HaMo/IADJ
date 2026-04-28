# Scripts del equipo IADJ

Scripts compartidos para que los 3 trabajemos en el repo sin tener
que borrar y re-clonar cada vez.

## Que hace cada uno

| Script        | Para que sirve                                                |
|---------------|---------------------------------------------------------------|
| `pull.bat`    | Trae los cambios del repo (git pull). Doble clic.             |
| `push.bat`    | Sube tus cambios al repo (git add + commit + push). Doble clic. |
| `reset-unity.bat` | ULTIMO RECURSO si Unity se rompe (borra cache local).     |
| `pull.sh` / `push.sh` | Versiones para Mac/Linux.                             |

## Workflow del dia a dia

### Para empezar a trabajar
1. Cierra Unity.
2. Doble clic en `pull.bat`.
3. Abre Unity. Lo nuevo aparece automaticamente.

### Para subir tu trabajo
1. Cierra Unity (recomendado).
2. Doble clic en `push.bat`.
3. Te pedira un mensaje de commit. Pon algo descriptivo (ej:
   "Anade modo guerra total"), no "cosas" o "cambios".
4. Listo. Tus compañeros ya pueden hacer pull.

### Si push falla porque alguien subio antes
- Ejecuta `pull.bat` primero.
- Luego vuelve a hacer `push.bat`.

### Si Unity se rompe (escena no carga, errores raros)
1. Cierra Unity.
2. Doble clic en `reset-unity.bat`.
3. Abre Unity. La primera vez tarda varios minutos.

Esto NO toca tu codigo ni tus assets, solo borra la cache local.

## Reglas para evitar conflictos

- **No editeis la misma escena `.unity` a la vez.** Avisad por
  Discord/grupo antes de tocar la escena principal.
- **Cerrad Unity antes de pull/push.** Unity puede tener ficheros
  locked y dar errores.
- **Mensajes de commit descriptivos.** Si no sabes que poner, "fix",
  "cambios" y "wip" no valen.

## Sobre OneDrive (IMPORTANTE)
El proyecto vive dentro de `OneDrive\...`. Es **mala idea** para Unity:
OneDrive sincroniza ficheros que Unity esta escribiendo y causa
corrupciones aleatorias. Cuando podais, mover el repo a `C:\Dev\IADJ\`
o similar.

## Como saber si los scripts estan funcionando bien
Si despues de `pull.bat` veis que en `git status` aparecen ficheros
tipo `.csproj`, `.sln` o `Logs/` como modificados, algo se cuelo de
nuevo. Avisad y los untrackeamos:
```
git rm --cached <fichero>
git commit -m "Untrack X"
git push
```
