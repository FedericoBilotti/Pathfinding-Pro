# Pathfinding-Pro
An advanced, high-performance implementation of the A* algorithm for Unity.  
Built with JobSystem + Burst for maximum speed and scalability.

## ✨ Features:

- **Multi-threaded pathfinding**: Parallel path calculation with Unity JobSystem + Burst.
- **Grid-threaded creation**: Supports multithread creation of the grid.
- **Grid-based navigation**: Supports 2D.
- **Optimized memory usage**: Precomputed grids stored and reused for different agents.
- **Custom Native Priority Queue**: High-performance open list implementation.
- **Scalable performance**: Tested with 300+ agents in a 280x115 grid (≈ 30,200 nodes).
- **Schedules Requests**: Request paths without blocking the main thread.
- **Weighted paths**: Supports different movement costs per node for more realistic navigation.

## 🛠️ Roadmap
- **Multi-level support**
- **Dynamic obstacles**
- **RVO / ORCA local avoidance**
- **3D volumetric navigation**
- **Agent-size awareness**
- **(Optional) RVO integration**.

## Final Grid Creation
![Grid Creation](https://github.com/user-attachments/assets/1ca34e03-0c87-4646-8eb0-231a13a908ea)

## Implementation of margin
![Grid Creation Gif](https://github.com/user-attachments/assets/296ac2ce-369f-43b0-93f3-bbb11243205a)

## Demo of the pathfinding requesting every frame
![Demo pathfinding Gif](https://github.com/user-attachments/assets/c351143a-9db6-4a73-bd8f-a0ee960639d5)

## Profiler of the pathfinding requests
![Multithreading Profiler](https://github.com/user-attachments/assets/afb8c8e8-006e-4116-b54d-1150a5f61f1a)
