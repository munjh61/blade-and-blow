using System;

namespace DTO.Auth
{
    [Serializable]
    public enum Mode
    {
        SINGLE, TEAM, PRIVATE
    }

    [Serializable]
    public class GetRecordResp
    {
        public RecordInfo[] recordInfo;
    }

    public class RecordInfo
    {
        public string weapon;
        public long win;
        public long lose;
        public long kill;
        public long death;
        public long damage;
    }
}