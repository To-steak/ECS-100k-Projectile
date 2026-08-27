# ECS 100k Projectile
Entity Component System을 사용해 씬에 100,000 개의 발사체와 2,000개의 피격체가 존재할 때, 충돌 방식에 따른 최적화 정도를 측정한 프로젝트입니다.

본 프로젝트에서는 공간 분할 방법인 HashMap을 사용해 적의 좌표를 기록하고 총알 근처 격자만 계산하는 것으로 성능 개선을 이룰 수 있었습니다.

추가로 근처 HashMap에 적이 존재하지 않는다면 HashMap 조회를 하지 않도록 적의 유무를 나타내는 Bit 배열을 사용했습니다.

Bit 조회에서 적이 없는 HashMap이라면 조회를 건너뛰므로 더 나은 성능을 기대할 수 있었습니다.

정리하면 다음과 같습니다.
1. WORST: 씬(Scene) 내 모든 발사체와 피격체를 이중 반복문으로 충돌 검사를 합니다.
2. BEST: 씬(Scene)을 격자로 분할하고 `HashMap`에 저장한 후 발사체 근처 9개(3 * 3)의 격자만 반복문으로 충돌 검사를 합니다.
3. BIT: 씬(Scene)을 격자로 분할하고 격자마다 적의 유무를 나타내는 비트 배열을 추가하고 발사체 근처 9개의 격자를 비교하되 적이 없는 격자라면 검사를 건너뜁니다. 

측정에 사용한 컴퓨터 사양은 다음과 같으며 빌드 후의 실행 결과를 기준으로 삼았습니다.
- CPU: 12th Gen Intel(R) Core(TM) i5-12600KF(3.70 GHz)
- GPU: NVIDIA GeForce RTX 3060 Ti
- RAM: 16.0GB

결과는 다음과 같았으며 BIT 방법이 더 좋은 성능을 보여주었습니다.

|방법|5초 평균 프레임|
|:-------|:-------|
|WORST|4.4 ~ 4.5 fps|
|BEST|76.6 ~ 76.8 fps|
|BIT|125.2 ~ 126.4 fps|

더 자세한 내용은 [Devlog](https://to-steak.github.io/)를 확인해주세요.
- [총알 10만 개 충돌시켜보기](https://to-steak.github.io/dots/dots_01.html)

# Demo - 아래 이미지 클릭 시 YouTube로 이동합니다.
[![ECS 100k Projectile Demo](https://img.youtube.com/vi/8JQztp0wtAY/maxresdefault.jpg)](https://youtu.be/8JQztp0wtAY)