# Pathfinding-Pro
Multithreading pathfinding.

Features:

- **Multi-threaded pathfinding**: Parallel path calculation with Unity JobSystem + Burst.
- **Grid-based navigation**: Supports 2D.
- **Optimized memory usage**: Precomputed grids stored and reused for different agents.
- **Custom Native Priority Queue**: High-performance open list implementation.
- **Scalable performance**: Tested with 300+ agents in a 280x115 grid (≈ 30,200 nodes).
- **Schedules Requests**: Request a path without blocking the main thread.

## 🛠️ Roadmap
- **Multi-level support**
- **Dynamic obstacles**
- **RVO / ORCA local avoidance**
- **3D volumetric navigation**
- **Agent-size awareness**
- **(Optional) RVO integration**.

![Grid Creation Gif]()
![Grid Creation](https://imgur.com/a/m9iLUni)
![Demo pathfinding Gif](https://imgur.com/a/lBxbR0K)
![Multithreading Profiler](https://imgur.com/a/r2VGlck)
