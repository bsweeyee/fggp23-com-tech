# FG23-ComTech: Performance Aware Shooter
![Game.png]()

## Description
A simple space shooter where you shoot waves of enemies that spawn and come towards you.

## Features
1. Player controller
2. Shooting
3. Waves of spawning enemy

## Optimizations
### Fixed size Heap memory allocations on startup
![Profiler_memory_2.png](<link> "Memory allocation to Entity at start of game")

Main idea is similar to Object Pooling in OOP
When starting up the game, it allocates all the memory for the Projectile and Enemy entities.

![Profiler_memory_1.png](<link> "Entities allocated memory remains constant throughout the game")

Then in the FireProjectileSystem and EnemySpawnSystem Jobs, we get a copy of the entity and set their states accordingly.

#### Why do this?
The main motivating reason to do this is because I wanted this game to spawn huge batches of entities per frame. If we spawn entities via EntityManager.Instantiate, we incur [Structural Changes](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/concepts-structural-changes.html).

Structural Changes from Destroying and Instantiating needs to be done in the main thread, leading to stalling when instantiating enemies.

#### How it is done
1. At the start, I make a rough estimation of how many projectiles / enemies to instantiate and proceed to spawn them.
    - For enemies, take the maximum spawn count and multiply it by a hard coded value as an [estimation]().
    - For projectile, [calculate the upper bound]() of how much projectile can be on screen at once. ( we can do this because we know the projectile spawn rate, count and distance it needs to travel before despawning )  
2. Attach 2 IEnableableComponents to 2 different IComponentData. In my case:
    - ProjectileTag and EnemyTag
    - ToSpawnFlag
3. In the main thread, [retrieve enemies that have their respective Tags disabled]().
4. Then set both the Tag and ToSpawnFlag to true
5. In the parallel job, we will be able to retrieve enemies that have the ToSpawnFlag set to true, and [run the initialization logic]().
6. Then we set the ToSpawnFlag back to false to prevent it from being used again

### Parallel scheduling with Jobs
#### Why do this?
Many operations that are done in this game needs to be done while looping over a bunch of IComponentData. 
However, we can structure the data such that for each loop, the current iteration does not depend on the previous iteration. (i.e each iteration is independent of each other)

#### How it is done
**Spawning**
This was described in the *Fixed size Heap memory allocations on startup* Section. 
But the key idea is figuring out that we can avoid looping and spawning entity one by one using the IEnableableComponent that flags them to be spawned, then defer the spawning in the parallel job later.

**Moving**
Using the IEnableableComponent to filter out if the Enemy / Projectile is on screen on not, we can then carry out the movement logic in parallel.

**Collision**
There are 2 collisions systems:
- EnemyProjectile
- EnemyPlayer

In both cases, the parallelized part is the enemy collision since there should be more enemies then both projectile and player at any one point.

## Other optimizations to look into
![Profiler_memory_3.png](<link> "Current profiler shows EnemyProjectileSystem and EnemyMoveSystem can be improved. This is at ~10000 enemy entities spawned at once")

### Improving collision checks
![EnemyProjectileCollisionSystem.png](<link> "At large enemy count, this system takes up one of the most processing time")
A big performance hit right now comes from the projectile-enemy collisions. Even though we already parallelize a single projectile-enemy check, we still have to loop through each projectile.

Some ways to cut down collision checks could be
1. Only check for collisions within camera view bounds plus some offset amount. Right now, I naively check for all enemies. Although the effects of this may not be as pronounced because of how the AI works.
2. Relating to the 2nd point, the AI simply moves to the player's current position. This means Enemy entities tend to crowd and accumulate together. Implementing AI to separate entities ( such as boiding ) or a state that stops the chase would likely help reduce collision checks
3. Applying a space partitioning algorithm like QuadTrees so that we can skip unnecessary checks.

## Conclusion
