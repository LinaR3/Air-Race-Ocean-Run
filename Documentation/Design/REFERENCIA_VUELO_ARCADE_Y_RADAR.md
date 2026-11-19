# Referencia de vuelo arcade y navegación

## Qué tomamos como referencia

- La guía oficial de **ACE COMBAT 7** ubica el radar en la esquina inferior izquierda y explica que alejar su zoom ayuda a entender el área completa. También describe el giro cerrado High-G como una combinación de frenado y giro. Fuente: https://www.bandainamcoent.com/pt_br/news/8-steps-to-fly-towards-victory-in-ace-combat-7
- El prototipo de Unity de **b3agz, “Simple Physics Based Plane Controller in Unity”**, recorre controles, cabeceo/alabeo/guiñada, cámara y HUD de velocidad/altitud. Se usa como material de aprendizaje, no como una simulación certificada. Video: https://www.youtube.com/watch?v=fThb5M2OBJ8

## Adaptación al juego de Air Race

No copiamos el combate, armas ni la complejidad de una simulación. Para la carrera usamos estas señales:

- **Vista:** una cámara trasera baja, con el horizonte nivelado. La inclinación del avión se ve dentro del encuadre; no se usa la vista alta anterior.
- **Trayectoria:** el rumbo real de vuelo sigue la orientación del avión. W/S cabecean el avión; A/D inclinan las alas y el rumbo cambia a medida que el avión se inclina. Así la elevación y el giro dejan de ser desplazamientos independientes que parecían deslizar el modelo.
- **Freno y giro cerrado:** mantener Left Shift reduce la velocidad y cierra el giro mientras A o D están presionadas. No es obligatorio para completar la ruta.
- **Power-up:** Espacio conserva la lata/impulso y la pirueta, según la regla del juego.
- **Navegación:** radar heading-up abajo a la izquierda: puerta activa amarilla, ruta restante clara, puertas superadas apagadas y latas como rombos. La puerta activa también muestra distancia en metros.
- **HUD:** velocidad y altitud arriba a la izquierda, tiempo arriba al centro, puntuación arriba a la derecha, energía y cargas abajo a la derecha. Se retiraron el contador central grande y la fila vieja de controles.
- **Colisiones:** un golpe aplica un único retroceso según la normal del contacto, con una breve exclusión de colisión para separar el avión. La recuperación no fuerza un giro lateral alrededor del pilón.
- **Latas:** quedan apenas por encima del corredor entre puertas y la luz se revela desde más lejos. El radar las distingue de los checkpoints.

## Controles actuales

| Tecla | Acción |
| --- | --- |
| W / S | Cabecear y subir / bajar siguiendo el rumbo del avión |
| A / D | Inclinar y girar a izquierda / derecha |
| Left Shift | Frenar; mantenida junto al giro cierra más la curva |
| Space | Activar una carga disponible y ejecutar la pirueta |
| R | Reintentar después de terminar |

No agregamos diez teclas a una experiencia para pantallas de promoción. Primero se debe confirmar la sensación de vuelo en Play Mode; después se puede decidir si una palanca analógica o joystick merece un esquema alternativo.

## Verificación

La compilación por lotes de Unity 6 terminó correctamente el 18 de noviembre de 2026. Las cuatro pruebas Play Mode que ya existían también pasan: rebote y separación del pilón, cámara trasera, radar/puerta activa y baliza de lata. El resultado completo está en `Documentation/QA/airrace-flight-radar-collision-playmode-tests.xml`. Eso valida esos comportamientos programáticos, pero no sustituye probar con teclado el tacto del giro y la altura en el editor.
