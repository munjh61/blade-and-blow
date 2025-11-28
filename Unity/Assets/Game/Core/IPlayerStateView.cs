using Game.Domain;

// 클라이언트 정보에 대한 접근 참조 인터페이스
public interface IPlayerStateView
{
    bool TryGet(int actor, out PlayerInfoData d);
}
