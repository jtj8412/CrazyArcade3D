# CrazyArcade3D Game

![Game Screenshot](https://jtj8412.github.io/resources/imgs/CrazyArcade3D/Main.png)

## Introduction

국내 온라인 게임인 크레이지아케이드를 3D로 재구성하여 제작한 게임입니다.  
물폭탄을 통해 블록을 부수고 아이템을 획득하여 능력치를 올리고 다른 플레이어와 경쟁하는 방식입니다.  
최대 4인 플레이가 가능합니다.

## Development

개발 기간: 2020.07.01 ~ 6주  
개발 환경: Unity, Visual Studio, Photon Cloud  
개발 언어: C#, PHP  
개발 팀원: 2명  
담당 업무: 클라이언트, 서버, 기획 등 ( 로그인 시스템을 제외한 모든 업무 )

## Game

![Lobby Screenshot](https://jtj8412.github.io/resources/imgs/CrazyArcade3D/Lobby.png)  
### Lobby
① 서버에 개설된 방 정보입니다. 클릭하면 해당 방 내부로 입장합니다.  
② 방을 개설할 때 입력해야할 란으로 방 제목입니다.  
③ 게임 내부에서 사용할 닉네임입니다.  
④ 방을 개설할 때 클릭해야 할 입력 버튼입니다
#     

![Room Screenshot](https://jtj8412.github.io/resources/imgs/CrazyArcade3D/Room.png)
### Room  
① 방에 참가한 플레이어들의 닉네임입니다.  
② 플레이어들이 소통할 수 있는 채팅창입니다.  
③ 게임 시작 버튼으로 방장에게만 활성화 됩니다.  
#  

![Player Screenshot](https://jtj8412.github.io/resources/imgs/CrazyArcade3D/Player.png)  
![Item Screenshot](https://jtj8412.github.io/resources/imgs/CrazyArcade3D/Item.png)  
![Bomb Screenshot](https://jtj8412.github.io/resources/imgs/CrazyArcade3D/Bomb.png)
![Block Screenshot](https://jtj8412.github.io/resources/imgs/CrazyArcade3D/Block.png)  
### Object
① 플레이어가 조종하는 캐릭터이며 캐릭터간 구분을 위해 방에 들어온 순서대로 모자 색상이 배정됩니다.  
② 아이템이며 각각 물줄기의 크기를 늘리는 효과, 최대 물폭탄 설치 갯수를 늘리는 효과, 캐릭터의 이동속도를 늘리는 효과가 있습니다.
③ 물폭탄과 물줄기이며 물폭탄 설치 후 일정시간이 지나면 X, Y, Z 세 방향으로 물줄기가 뻗어나갑니다.  
④ 블록이며 물폭탄으로 파괴해서 아이템을 획득할 수 있고 캐릭터가 이동하는 방향으로 밀 수도 있습니다.
#  

![Control Screenshot](https://jtj8412.github.io/resources/imgs/CrazyArcade3D/Control.png)  
### Control
○ W/A/S/D:      이동  
○ SpaceBar:     점프  
○ R:            카메라 반전
○ Left Click:   물폭탄 설치 / 관전 플레이어 변경 (죽고난 후)  
○ Right Click:  펀치 (기절 및 넉백)  
○ Wheel:        카메라 줌

