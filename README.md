# Fireworks Mini (Unity) - prueba para comparar con la version web

Proyecto Unity minimo: todo el juego se crea por codigo (`Assets/Scripts/FireworksGame.cs`), sin escena ni modelos.
`Assets/Editor/BuildScript.cs` crea la escena vacia y compila el APK en GitHub Actions.

## Pasos
1. Crea un repositorio en GitHub y sube TODO el contenido de esta carpeta (incluida `.github`).
2. Licencia de Unity para GitHub Actions: en Settings > Secrets and variables > Actions agrega
   `UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD`. La activacion de la licencia personal
   se explica en https://game.ci/docs/github/activation (Unity cambia estas reglas de vez en cuando; revisa esa pagina).
3. Ve a Actions > "Build APK (Unity)" > Run workflow. La primera vez tarda bastante (20-40 min).
4. Descarga el artefacto `FireworksMini-apk`, descomprime e instala el `.apk` en el celular (permite fuentes desconocidas).

## Controles
Joystick izquierdo: moverte. Arrastrar a la derecha: mirar.
PONER: coloca el fuego donde apuntas. MANO: agarrar/soltar. FUEGO: enciende lo que apuntas.
SALTO, TIPO (cambia de fuego), HORA (dia/noche). Arriba a la izquierda se ve el FPS.

## Como comparar con la version web
Prueba las dos en el mismo celular y fijate en: FPS con muchos fuegos a la vez (todos los morteros),
tiempo de carga, tamano (APK vs pagina), y que tan comodos se sienten los controles.

## Nota
No pude compilar este proyecto aqui, asi que puede tener errores. Si falla el workflow, copia el error del log y se corrige.
