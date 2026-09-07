# ECS 100k Projectile
발사체 100,000체와 피격체 2,000체의 세계에서 충돌을 시뮬레이션 할 때, 해시맵 조회 이전에 비트 필터링으로 해시 조회 비용을 아낄 수 있는지 실험

## 측정 환경
- Unity ECS, 단일 메인 스레드 (병렬 없이 비트 필터 유무만 비교)
- 총알이 완전히 확산된 뒤 개발 빌드에서 Profiler 스냅샷 확인
- CPU : 12th Gen Intel(R) Core(TM) i5-12600KF(3.70 GHz)
- GPU : NVIDIA GeForce RTX 3060 Ti
- RAM : 16GB

## 결과

| 구간 | 비트 OFF | 비트 ON | 차이 |
| --- | --- | --- | --- |
| CPU 총합 | 14.84ms | 9.73ms | -5.11ms (-34%) |
| SimulationSystemGroup | 11.94ms | 6.96ms | -4.98ms (-42%) |
| CollisionSystem | 10.27ms | 5.53ms | -4.74ms (-46%) |
| Collision.Sync | 2.99ms | 2.63ms | -0.36ms |
| Collision.Alloc | 0.003ms | 0.003ms | 0ms |
| Collision.BuildGrid | 0.017ms | 0.021ms | +0.004ms |
| Collision.Query | 7.26ms | 2.88ms | -4.38ms (-60%) |
| EndSimulationECB | 0.829ms | 0.489ms | -0.34ms |

## 용어 정리
| 마커 | 의미 |
| --- | --- |
| SimulationSystemGroup | 매 프레임 ECS 전체가 차지하는 부분 |
| CollisionSystem | SimulationSystemGroup에서 시스템이 차지하는 부분 |
| Collision.Sync | CollisionSystem에서 다른 Job을 기다리는데 걸린 시간 |
| Collision.Alloc | CollisionSystem에서 해시 맵 할당에 걸린 시간 |
| Collision.BuildGrid | CollisionSystem에서 격자 생성에 걸린 시간 |
| Collision.Query | CollisionSystem에서 총알 주변 9개 격자를 순회하는데 걸린 시간 |
| EndSimulationECB | SimulationSystemGroup에서 충돌한 적을 파괴하는데 걸린 시간 |