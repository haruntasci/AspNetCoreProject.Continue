﻿using NETCore.Encrypt;

namespace AspNetCoreProject.Continue.Helpers
{
    public interface IHasher
    {
        string DoMD5HashedString(string s);
    }

    public class Hasher : IHasher
    {
        private readonly IConfiguration _configuration;

        public Hasher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string DoMD5HashedString(string s)
        {
            string md5Salt = _configuration.GetValue<string>("AppSettings:MD5Salt");
            string saltedPassword = s + md5Salt;
            var hashed = EncryptProvider.Md5(saltedPassword);
            return hashed;
        }
    }
    public class Hasher2 : IHasher
    {
        public string DoMD5HashedString(string s)
        {
            throw new NotImplementedException();
        }
    }
}
