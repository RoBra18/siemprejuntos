# Entrega de DimDam para Google Play

## Archivo de subida

- Android App Bundle: `Builds/Android/DimDam.aab` (se genera desde **Tools > DimDam > Crear AAB Android** en Unity).
- Identificador: `com.winnergamejam.dimdam`.
- Nombre visible: `DimDam` (la pantalla principal dice «Siempre Juntos»).
- Versión inicial: `1.0.0` / código `1`.
- Android mínimo: API 26; objetivo: API 36; arquitectura: ARM64; motor: IL2CPP.
- Tamaño de descarga estimado por Unity: 166 MB (el archivo AAB ocupa aproximadamente 175 MB).
- Integridad del AAB final, SHA-256: `2A0C4A6D427BCFD5037A0F1ACCD328008EBA8C8A8629A8BC936E9084F9E89100`.

El AAB se firma con la clave de subida creada en `C:\Users\PC\DimDam Play Store Handoff\DimDam-upload.jks`. El archivo `signing.txt` en esa misma carpeta contiene alias y contraseñas. Entréguense esos dos archivos al responsable de la cuenta por un canal privado y consérvese una copia de seguridad. No publicar ni compartir la clave en un repositorio.

Los recursos de la ficha están en esta carpeta: `icon-512.png`, `feature-1024x500.jpg` y dos capturas en `screenshots/`. El texto propuesto para la ficha está en `FICHA_PLAY.md`. La política de privacidad propuesta está en `PRIVACIDAD.md`; el menú del juego ya incluye un resumen de privacidad.

**Subir únicamente `DimDam.aab` a Play Console.**

## Qué hace el multijugador

Dos personas pueden jugar con controles táctiles en una pantalla. En modo de dos dispositivos, ambos se conectan a la misma red Wi-Fi o punto de acceso; el anfitrión muestra la partida y el segundo teléfono actúa como mando de Pipro. El puerto local es TCP 27841. No hay servidores ni partidas por internet.

## Pasos en Play Console

1. Crear un juego nuevo con el nombre y el identificador anteriores. Verificar que el identificador no esté ya ocupado antes de subir el primer paquete: no podrá cambiarse después de publicar.
2. Activar Play App Signing. Subir `DimDam.aab` primero a una pista de prueba interna o cerrada. Google Play usará el certificado de subida para comprobar futuras versiones.
3. Completar la ficha: título, descripción corta y larga, categoría Juego, icono de 512×512, imagen destacada de 1024×500 y capturas reales del juego en móvil. Añadir una URL pública para la política de privacidad y mostrarla dentro de la app.
4. Completar «Contenido de la app»: seguridad de datos, público objetivo, clasificación por edades, anuncios (el proyecto actual no integra anuncios), acceso a la app y demás declaraciones que solicite la cuenta. La declaración de datos debe revisarse contra el AAB final y cualquier servicio que el responsable agregue.
5. Si la cuenta es personal y fue creada después del 13 de noviembre de 2023, realizar la prueba cerrada requerida por Google: al menos 12 participantes inscritos durante 14 días continuos antes de solicitar acceso a producción.
6. Revisar el informe de pruebas automáticas de Play Console y probar la conexión LAN en dos teléfonos físicos antes de lanzar al público.

## Validación realizada sin teléfonos

- Unity comprobó que todas las escenas de campaña cargan, que las escenas cooperativas tienen dos jugadores y que los controles táctiles de ambos se asignan independientemente.
- Una prueba TCP de bucle local verificó los dos sentidos del modo multijugador: el anfitrión recibe controles y el invitado envía joystick, salto y ulti.
- Se jugó una escena en el editor sin cierres ni excepciones. El AAB final se compiló correctamente y se revisaron su manifiesto (identificador, API 26/36, permiso de internet) y certificado de subida.
- Se intentó una compilación ARM de 32 bits para el emulador x86 disponible; Unity devolvió un error interno de su sistema de compilación antes de generar el APK. Por ello no se ha validado el arranque en emulador.
- Antes del lanzamiento público falta probar el AAB instalado en móviles reales, la conexión Wi-Fi entre dos teléfonos y el informe de Play Console.

## Próximas versiones

Mantener el mismo identificador y la misma clave de subida. Incrementar `AndroidBundleVersionCode` en cada AAB nuevo. No perder el archivo de la clave ni sus contraseñas.

## Fuentes oficiales consultadas

- API objetivo: https://support.google.com/googleplay/android-developer/answer/11926878
- Firma: https://support.google.com/googleplay/android-developer/answer/9842756
- Gráficos y capturas: https://support.google.com/googleplay/android-developer/answer/9866151
- Prueba cerrada: https://support.google.com/googleplay/android-developer/answer/14151465
- Política de privacidad: https://support.google.com/googleplay/android-developer/answer/18258653
