
# Red de Transporte Urbano — Proyecto de Laboratorio No. 2

**Curso:** Estructura de Datos II · Ing. Manuel Gustavo Rojas  
**Lenguaje:** C# (.NET 10, Windows Forms)  
**Modalidad:** Grupal  

---

## 1. Descripción del proyecto y escenario

Sistema de gestión de la red de transporte urbano de Quetzaltenango y municipios cercanos (Almolonga, Salcajá y Cantel). Permite registrar **paradas** y **rutas** entre ellas, buscar paradas rápidamente, autocompletar nombres, recorrer la red, calcular la **ruta óptima** (por tiempo o costo) y saber si dos paradas pertenecen a la misma **zona conectada**.

Las paradas se muestran sobre un **mapa real (OpenStreetMap)**: al agregar una parada haciendo clic en el mapa, el nodo queda en esa ubicación geográfica exacta (latitud/longitud).

---

## 2. Estructuras de datos utilizadas y justificación

| Estructura | Archivo | Qué resuelve | Por qué se eligió |
|---|---|---|---|
| **Tabla Hash** (encadenamiento, rehash al 75 % de carga) | `Estrucuturas/TablaHash.cs` | Búsqueda de una parada por código/ID | Acceso promedio O(1); la búsqueda por código es la operación más frecuente |
| **Grafo** (lista de adyacencia) con **BFS, DFS y Dijkstra** (montículo binario propio) | `Estrucuturas/Grafo.cs` | Rutas entre paradas, recorridos y camino óptimo | La red es dispersa (pocas rutas por parada): la lista usa O(V+E) de memoria, la matriz O(V²) |
| **Union-Find** (compresión de camino + unión por rango) | `Estrucuturas/Unionfind.cs` | Conectividad: ¿están dos paradas en la misma zona? | Consulta y unión en tiempo casi constante O(α(n)); evita recorrer el grafo en cada pregunta |
| **Trie** (hijos en lista enlazada, sin tildes ni mayúsculas) | `Estrucuturas/Trie.cs` | Autocompletar nombres y códigos por prefijo | Costo proporcional al largo del prefijo, no a la cantidad de paradas |

Las estructuras son **propias** (no se usan `List`, `Dictionary` ni `HashSet` en el núcleo).  
La clase `Servicios/RedTransporte.cs` las coordina: al agregar o eliminar una parada se actualizan la Tabla Hash, los dos Tries (nombres y códigos), el Grafo y el Union-Find a la vez.

---

## 3. Instrucciones de ejecución

**Requisitos:** Windows, **.NET SDK 10** (o el que indique `net10.0-windows` en el `.csproj`).

```powershell
cd Proyecto2EstructurasDatosII-main      # la carpeta que contiene el .csproj
dotnet clean
dotnet run
```

También puede abrirse la carpeta en Visual Studio / VS Code y presionar *Ejecutar*.

El mapa real necesita Internet la primera vez (los mosaicos se guardan en `%LOCALAPPDATA%\Proyecto2EstructurasDatosII\mosaicos`); sin conexión se muestra el esquema de la red.

---

## 4. Datos de prueba (ubicación y formato)

Carpeta `Recursos/` (se copia junto al ejecutable y se carga automáticamente al iniciar):

* `paradas.csv` — **30 registros principales**. Formato: `codigo,nombre,latitud,longitud`
* `rutas.csv` — **40 conexiones**. Formato: `origen,destino,tiempo,costo,bidireccional`

La red forma **4 zonas**: Quetzaltenango (20 paradas), Almolonga (4), Salcajá (3) y Cantel (3).  
Las coordenadas son aproximadas y sirven para demostrar el sistema.

---

## 5. Funcionalidades

* **Paradas:** alta, baja, búsqueda por código (Hash) y existencia.
* **Mapa real:** ubicación de cada parada; agregar parada con un clic (o clic derecho) y eliminar desde el mapa.
* **Rutas:** alta/baja con tiempo, costo y sentido (uno o dos sentidos).
* **Buscar ruta:** Dijkstra por tiempo o costo; valida primero con Union-Find que exista conexión.
* **Recorridos:** BFS y DFS desde cualquier parada.
* **Zonas conectadas:** grupos de paradas (Union-Find).
* **Autocompletar:** por nombre o código (Trie).
* **Estructuras:** visualización interna de cada estructura, incluido el dibujo del grafo.
* **Datos y archivos:** carga de CSV (paradas y rutas) con registro de líneas ignoradas.

---

## 6. Documentación

Los documentos del proyecto se encuentran en la carpeta `Documentos/`:

* [`Documentos/Manual de Usuario.pdf`](Documentos/Manual%20de%20Usuario.pdf) — uso del sistema paso a paso.
* [`Documentos/Manual Técnico Proyecto 2 Estrucuturas II (1).pdf`](Documentos/Manual%20Técnico%20Proyecto%202%20Estrucuturas%20II%20(1).pdf) — diseño, estructuras, justificación y trade-offs.

---

## 7. Equipo y aportes

| Integrante | Código | Módulo / aporte |
|---|---|---|
| **Morelia Gonzalez Ixcot** | 1536325 | **Parte 1 – Módulo de Datos y Almacenamiento Rápido** (Tabla Hash). Clases modelo (`Models`), implementación completa de la Tabla Hash (función de dispersión y manejo de colisiones), archivos CSV de prueba y servicio de carga de datos. |
| **Jackelin Sofia Vásquez Guzmán** | 1503725 | **Parte 2 – Módulo de Navegación y Algoritmos de Red** (Grafo). Estructura del grafo con listas de adyacencia propias, soporte de rutas unidireccionales, BFS, DFS y algoritmo de Dijkstra (ruta más rápida o económica). |
| **Keila Belinda Puac Gómez** | 1660625 | **Parte 3 – Módulo de Interfaz, Autocompletado y Conectividad Zonal** (Trie y Union-Find). Trie para autocompletado de nombres en tiempo real y Union-Find con compresión de caminos para verificar zonas conectadas. |

> Los *commits* del repositorio reflejan este reparto de responsabilidades.

---

## 8. Estructura del repositorio

```
Estrucuturas/   TablaHash, Grafo, Trie, UnionFind (implementaciones propias)
Models/         Parada, Ruta
Servicios/      RedTransporte (coordina las 4 estructuras)
Datos/          CargadorDatos (lectura de CSV)
Interfaz/       Formularios, mapa real, dibujo del grafo
Recursos/       paradas.csv, rutas.csv (datos de prueba)
Documentos/     Manual de Usuario y Manual Técnico (PDF)
```
```