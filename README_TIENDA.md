# 🎮 Sistema de Tienda de Slimes - Proyecto Final Game

## 📝 Descripción

Sistema completo de tienda de slimes implementado en Unity 2D para el juego "Proyecto Final Game". 

El sistema permite que el jugador:
- Presione la tecla **"T"** en la zona de tienda para abrirla
- Vea los 4 slimes disponibles con sus precios
- Seleccione un slime para ver sus detalles
- Compre slimes usando cristales como moneda

## 🎯 Características

✅ **Sistema de tienda funcional**
- Interfaz de usuario completamente implementada
- 4 slimes disponibles para comprar (Rosa, Planta, Fuego, Toxico)
- Precios configurables
- Sistema de cristales integrado

✅ **Interacción con el jugador**
- Detección de área de tienda con collider
- Tecla "T" para abrir la tienda
- Pausa del juego mientras la tienda está abierta
- Botón cerrar para salir

✅ **Gestión de recursos**
- Integración con ResourceManager existente
- Cobro automático de cristales
- Validación de dinero suficiente

✅ **Interfaz visual**
- Botones de selección de slimes
- Panel de información del slime seleccionado
- Contador de cristales disponibles
- Indicador visual de si puedes comprar (botón verde/rojo)

## 📁 Estructura del Proyecto

```
Assets/
├── Scripts/
│   ├── Systems/
│   │   ├── SlimeShopData.cs          ← Datos de la tienda
│   │   ├── ShopTrigger.cs            ← Detecta interacción
│   │   └── SlimeInventoryManager.cs  ← (Opcional) Inventario
│   ├── UI/
│   │   └── ShopUI.cs                 ← Pantalla de tienda
│   ├── Managers/
│   │   ├── GameManager.cs            (existente)
│   │   └── ResourceManager.cs        (existente)
│
├── Sprites/
│   ├── SlimeRosa.png
│   ├── SlimePlanta.png
│   ├── SlimeFuego.png
│   └── SlimeToxico.png
│
├── Prefabs/
│   └── UI/
│       └── SlimeButtonPrefab.prefab
│
└── Scenes/
    ├── MainScene.unity
    └── Tienda.unity
```

## 🚀 Instalación Rápida

### Paso 1: Scripts
Los scripts ya están creados en:
- `Assets/Scripts/Systems/SlimeShopData.cs`
- `Assets/Scripts/UI/ShopUI.cs`
- `Assets/Scripts/Systems/ShopTrigger.cs`

### Paso 2: Configuración en Unity
Sigue el **CHECKLIST_TIENDA.txt** para configurar:
1. Canvas con ShopUI
2. Zona de tienda (ShopArea)
3. Datos de los slimes
4. Sprites

### Paso 3: Sprites
Copia 4 imágenes PNG a `Assets/Sprites/`:
- `SlimeRosa.png`
- `SlimePlanta.png`
- `SlimeFuego.png`
- `SlimeToxico.png`

## 📚 Documentación

### 🟢 Para comenzar (5 minutos)
- **RESUMEN_RAPIDO.txt** ← Empieza aquí

### 🟡 Para implementar (30 minutos)
- **GUIA_TIENDA_SLIMES.txt** - Paso a paso
- **CHECKLIST_TIENDA.txt** - Lista de verificación

### 🔵 Para entender todo
- **MAPA_MENTAL_TIENDA.txt** - Arquitectura completa
- **REFERENCIA_SCRIPTS.txt** - Documentación de scripts
- **VISUALIZACION_UI.txt** - Cómo se ve la interfaz

### 🟣 Para problemas
- **SOLUCION_PROBLEMAS.txt** - FAQ y soluciones

### 📋 Para navegar
- **INDICE_MAESTRO.txt** - Índice de toda la documentación
- **GUIA_CREAR_SPRITES.txt** - Cómo crear/obtener sprites

## 🎮 Uso

1. **Presiona PLAY** en Unity
2. **Mueve tu personaje** a la zona de tienda
3. **Presiona "T"** para abrir la tienda
4. **Haz click** en un slime para verlo
5. **Haz click en "COMPRAR"** si tienes dinero
6. **Presiona "CERRAR"** para salir

## 🛠️ Configuración de Slimes

Cada slime tiene:
- **Nombre**: "Slime Rosa", "Slime Planta", etc.
- **Tipo**: Rosa, Planta, Fuego, Toxico
- **Precio**: 300, 250, 400, 350 (cristales)
- **Descripción**: Texto descriptivo
- **Sprite**: Imagen del slime

## 🔧 Personalización

### Cambiar precios
En `SlimeShopData`, modifica el campo "Price"

### Cambiar tecla
En `ShopTrigger`, modifica "Shop Key" (T por defecto)

### Agregar más slimes
1. Aumenta "Size" en `SlimeShopData`
2. Configura el nuevo slime
3. Crea su sprite

## 🐛 Solución de Problemas

**¿Presiono T pero no se abre la tienda?**
- Verifica que estés en la zona (ShopArea)
- Verifica que el collider tenga "Is Trigger"
- Verifica que tu personaje tenga tag "Player"

**¿El botón de compra no funciona?**
- Verifica que tengas suficientes cristales
- El botón debería estar VERDE si puedes comprar

**¿No veo las imágenes?**
- Verifica que los PNG tengan Texture Type = "Sprite (2D and UI)"

Más soluciones en **SOLUCION_PROBLEMAS.txt**

## 📊 Scripts Principales

### SlimeShopData.cs
Gestiona los datos de los slimes:
```csharp
GetAvailableSlimes()      // Obtiene todos los slimes
CanBuySlime(int)          // Verifica si puedes comprar
BuySlime(int)             // Compra un slime
```

### ShopUI.cs
Controla la interfaz visual:
```csharp
OpenShop()                // Abre la tienda
CloseShop()               // Cierra la tienda
CreateSlimeButtons()      // Crea botones de slimes
```

### ShopTrigger.cs
Detecta interacción del jugador:
```csharp
OnTriggerEnter2D()        // Detección de entrada
OnTriggerExit2D()         // Detección de salida
Update()                  // Detecta tecla T
```

## 🎨 Recursos Necesarios

### Sprites (4 imágenes PNG)
- Tamaño recomendado: 200x200 píxeles
- Formato: PNG con transparencia
- Ubicación: `Assets/Sprites/`

Opciones para obtenerlos:
1. **Descargar**: OpenGameArt.org, Itch.io
2. **Generar con IA**: ChatGPT + DALL-E, Midjourney, Stable Diffusion
3. **Crear tú mismo**: Aseprite, GIMP, Pixilart.com

Ver **GUIA_CREAR_SPRITES.txt** para más detalles.

## 🔗 Integración

### ResourceManager
El sistema se integra automáticamente con `ResourceManager` para:
- Obtener cantidad de cristales disponibles
- Restar cristales al comprar
- Mostrar contador actualizado

### GameManager
Se integra con `GameManager` para:
- Pausar el juego cuando se abre la tienda
- Reanudar cuando se cierra

## 📈 Próximas Mejoras (Opcionales)

- [ ] Sonidos de compra
- [ ] Animaciones del panel
- [ ] Efecto visual de compra exitosa
- [ ] Sistema de inventario visual
- [ ] Más tipos de slimes
- [ ] Sistema de misiones de tienda

## ⚙️ Requerimientos

- Unity 2023.2 o superior
- TextMeshPro (incluido en Unity)
- 2D Physics
- Input System

## 📝 Notas

- Los scripts están completamente funcionales y listos para usar
- No requieren modificación de código (solo configuración en Unity)
- Compatible con escenas múltiples
- Sistema de Singleton para managers persistentes

## 🎓 Conceptos Implementados

- **Singleton Pattern**: GameManager, ResourceManager, SlimeShopData
- **UI System**: Canvas, Buttons, Images, Text
- **Collider 2D**: Trigger para área de tienda
- **Input Handling**: Detección de tecla T
- **State Management**: Abrir/cerrar tienda
- **Resource Management**: Cristales, validación

## 📞 Soporte

Para problemas:
1. Consulta **SOLUCION_PROBLEMAS.txt**
2. Verifica **CHECKLIST_TIENDA.txt**
3. Lee **REFERENCIA_SCRIPTS.txt**

## ✨ Autor

Sistema de Tienda de Slimes - Proyecto Final Game
Versión 1.0 - Junio 2026

---

**¡Listo para implementar! Comienza con RESUMEN_RAPIDO.txt**
