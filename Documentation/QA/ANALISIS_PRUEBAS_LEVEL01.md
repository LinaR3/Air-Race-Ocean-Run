# Análisis y pruebas — Air Race Level 01

## Estado de esta revisión

Revisé los scripts de vuelo, checkpoints, gestión de carrera, el prefab del avión, el prefab de los pilones y la escena `AirRace_Level01_Map`.

La carrera usa un avión de 32 m/s, seis checkpoints a 200–206 m entre centros (unos 6,3 s de vuelo recto entre ellos), un océano de 4,8 km y una barrera invisible a 900 m. Por geometría, la separación de la ruta es razonable para la duración acordada de 60 s. El problema más urgente estaba en la respuesta del choque.

## Diagnóstico del atasco

`OnCollisionEnter` calculaba un rebote, pero `FixedUpdate` reescribía la velocidad del avión hacia delante en el siguiente paso de física. Así el avión podía quedar empujando contra el pilón, sin tiempo para separarse. El prefab del pilón también usa colliders de malla en varias bandas, lo que puede producir contactos menos suaves que un solo collider primitivo.

La respuesta conserva el impulso de salida durante 0,28 s, vuelve gradualmente al vuelo normal durante 0,22 s y gira el avión 30° hacia una trayectoria de escape. La prueba encontró que solo rebotar hacia atrás podía dejarlo alineado para volver a tocar el mismo pilón; la corrección incluye una evasión lateral. Cada pilón desactiva sus bandas de colisión por malla y genera una cápsula para el cono y una caja para el flotador, ambos con fricción mínima. El daño tiene un enfriamiento de 0,65 s y se comunica con un aviso de impacto durante 1,1 s. La lata se hizo más grande y su aro visible pasó de 10,4 m a 13 m de diámetro exterior en la primera iteración.

## Pruebas ejecutadas en Unity 6.3

Ejecuté las pruebas de Unity Play Mode en una copia aislada del proyecto. El choque ocurrió mediante el callback real `OnCollisionEnter` contra el prefab del pilón, no mediante una invocación simulada del método. También se comprobó el encuadre de cámara:

| Prueba | Resultado |
| --- | ---: |
| Avance recto durante 1 s | 32,00 m |
| Giro a la derecha durante 0,5 s | 17,00° |
| Ascenso durante 0,5 s | 2,30 m |
| Contacto real con el pilón, callback | Detectado a los 0,06 s |
| Velocidad de salida en dirección opuesta | 16,13 m/s |
| Penalización de un impacto | 18 de energía, una vez |
| Distancia al pilón al iniciar el escape → tras 1,2 s | 5,26 → 19,08 m |
| Velocidad tras terminar la recuperación | 32,00 m/s |
| Colliders del pilón | 1 cápsula + 1 caja; bandas mesh desactivadas |
| Cámara anterior: siguiente puerta en pantalla | viewport (0,35; 0,60), delante del avión |

Las dos pruebas de Play Mode pasaron: choque y rebote, y encuadre de cámara. El resultado queda en [airrace-playmode-test-results.xml](airrace-playmode-test-results.xml); el test reproducible está en `Assets/AirRace/Tests/PlayMode`.

![Captura de la escena de prueba con avión y pilones](unity-collision-recovery.png)

Resultados numéricos: [unity-mobility-collision-results.txt](unity-mobility-collision-results.txt).

## Ocho mejoras aplicadas

1. Mantener el impulso de separación del avión antes de restablecer velocidad de crucero.
2. Aplicar fricción mínima a los colliders de las piezas del pilón para reducir el enganche en las uniones de malla.
3. Poner un enfriamiento de 0,65 s al daño por impacto para que un mismo roce no descuente energía varias veces.
4. Dar señal visible de impacto y rebote durante 1,1 s.
5. Aumentar la legibilidad de la lata y su aro de energía para distinguirlos mejor a distancia.
6. Simplificar la colisión del pilón a una cápsula para el cono y una caja para el flotador, conservando el modelo visual.
7. Probar un choque real de Play Mode, detectar el segundo impacto que ocurría al volver a apuntar al pilón y añadir un desvío de 30°.
8. Acercar y elevar un poco la cámara, acelerar su seguimiento y confirmar que la puerta siguiente queda dentro del encuadre.

## Siguientes prioridades

1. **Balancear energía en una vuelta con control manual:** la geometría da aproximadamente 1.339 m, 41,8 s a 32 m/s y 72–82 de energía al llegar con los seis checkpoints; esa estimación ideal deja margen y no justifica subir el drenaje todavía. Hay que repetir la vuelta con correcciones reales, una puerta perdida y uso de latas.
2. **Aclarar la ruta y la meta:** resaltar puerta activa, siguiente puerta y llegada; mostrar feedback cuando una puerta se atraviese fuera de orden.
3. **Reemplazar el HUD temporal:** llevar tiempo, energía, puntos y checkpoint a una interfaz legible en pantallas grandes.
4. **Dar escala al horizonte:** añadir costa o siluetas lejanas discretas sin competir con el recorrido.
5. **Ampliar la matriz de choques:** probar lateral, ala, nariz y flotador a distintas alturas y velocidades, además de impactos repetidos.

## Límite de la verificación

`dotnet build Assembly-CSharp.csproj --no-restore` y `dotnet build Assembly-CSharp-Editor.csproj --no-restore` pasaron con 0 errores y 0 advertencias después de restaurar los assets NuGet del proyecto. Play Mode se ejecutó con el Test Framework de Unity en una copia aislada; la escena abierta de la usuaria no se regeneró ni se modificó durante las pruebas.

## Iteración siguiente: cámara, HUD y lata

La cámara principal ahora va centrada detrás y bastante elevada sobre el avión, con la vista dirigida hacia la próxima sección de ruta. `C` mantiene una alternativa más cercana. La prueba confirma que el avión queda centrado lateralmente y que el siguiente checkpoint se ve en pantalla, en viewport (0,44; 0,69).

El HUD temporal de `OnGUI` se sustituyó por una capa de pantalla escalable con paneles de alto contraste. Muestra tiempo, número del siguiente checkpoint, puntuación, barra de energía, cargas de lata y mensaje de impacto; al terminar presenta resultado y reintento. El diseño sirve para validar jerarquía y lectura en pantalla grande; la dirección final de arte/UI podrá sustituirse por el diseño de Figma.

La lata visual ahora es 33 % mayor, su aro mide 17 m de diámetro exterior, y el radio de recolección aumentó a 10 m. El aro pulsa mediante emisión; una luz amarilla se enciende y aumenta su alcance al acercarse el avión. El HUD muestra las cargas disponibles y pide buscar una lata cuando no hay ninguna.

Cuatro pruebas de Play Mode pasan en la copia aislada del proyecto: impacto/rebote real, encuadre de cámara, actualización numerada del HUD y tamaño/radio/baliza de la lata. El detalle está en [airrace-hud-camera-playmode-tests.xml](airrace-hud-camera-playmode-tests.xml).

## Prioridades pendientes

1. Probar una vuelta completa manual y balancear el drenaje de energía con los giros reales, las latas y algún checkpoint fallido.
2. Dar identidad visual al checkpoint activo, a los ya cruzados y a la meta, sin llenar el océano de indicadores.
3. Probar el giro arcade más responsivo para corregir la sensación de maniobra lenta antes de afinar cámara y HUD definitivamente.
4. Hacer la revisión visual del HUD a resolución de pantalla de promoción y reemplazar sus paneles provisionales con la dirección de Figma.
5. Añadir después escala de horizonte y detalles de costa.
