using System;

namespace DTO.Auth
{

    [Serializable]
    public class Nothing { }
    [Serializable]
    public class AccessTokenResp { public string accessToken; }
    [Serializable]
    public class RefreshTokenReq { public string refreshToken; }
    [Serializable]
    public class SignupReq
    {
        public string username;
        public string password;
        public string nickname;
    }

    [Serializable]
    public class LoginReq
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class LoginResp
    {
        public string username;
        public string nickname;
        public string accessToken;
        public string refreshToken;
    }
    [Serializable]
    public class MeResp {
        public string username;
        public string nickname;
    }
    [Serializable]
    public class NicknameReq { public string nickname; }
    [Serializable]
    public class PingResp { public string msg; }
}
