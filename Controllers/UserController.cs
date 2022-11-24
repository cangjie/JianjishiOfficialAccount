using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OA.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using OA;
using OA.Controllers;


namespace OA.Controllers.Api
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly SqlServerContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public UserController(SqlServerContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [HttpGet]
        public async Task<ActionResult<bool>> CheckToken(string token)
        {
            token = Util.UrlEncode(token);
            long currentTimeStamp = long.Parse(Util.GetLongTimeStamp(DateTime.Now));
            var tokenList = await _db.token.Where(t =>
            (t.state == 1 && currentTimeStamp < t.expire_timestamp
            && t.token.Trim().Equals(token.Trim()) && t.original_id.Trim().Equals(_settings.originalId.Trim())))
                .ToListAsync();
            if (tokenList.Count > 0)
            {
                string openId = tokenList[0].open_id.Trim();
                await CheckUser(openId.Trim());
                return true;
            }
            return false;
        }

        [NonAction]
        public string GetUserOpenId(string token)
        {
            //token = Util.UrlEncode(token);
            long currentTimeStamp = long.Parse(Util.GetLongTimeStamp(DateTime.Now));
            var tokenList = _db.token.Where(t =>
            (t.state == 1 && currentTimeStamp < t.expire_timestamp
            && t.token.Trim().Equals(token.Trim()) && t.original_id.Trim().Equals(_settings.originalId.Trim()))).ToList();
            if (tokenList.Count > 0)
            {
                return tokenList[0].open_id.Trim();
            }
            else
            {
                return "";
            }

        }

        [NonAction]
        public async Task<ActionResult<int>> SetToken(string token, string openId, int expireSeconds)
        {

            int userId = (int)(await CheckUser(openId)).Value;
            if (userId == 0)
            {
                return 0;
            }
            long currentTimeStamp = long.Parse(Util.GetLongTimeStamp(DateTime.Now));
            var tokenList = _db.token.Where(t => (
                t.original_id.Trim().Equals(_settings.originalId)
                && t.open_id.Trim().Equals(openId)
                && (t.state == 1 || t.expire_timestamp <= currentTimeStamp)
            )).ToList();
            for (int i = 0; i < tokenList.Count; i++)
            {
                Token tmpToken = tokenList[i];
                tmpToken.state = 0;
                _db.Entry(tmpToken).State = EntityState.Modified;
                _db.SaveChanges();

            }
            Token newToken = new Token()
            {
                id = 0,
                original_id = _settings.originalId.Trim(),
                open_id = openId.Trim(),
                token = token.Trim(),
                expire_timestamp = currentTimeStamp + 1000 * (expireSeconds - 720),
                user_id = userId,
                state = 1
            };
            _db.token.Add(newToken);
            _db.SaveChanges();

            return newToken.id;
        }

        [HttpGet]
        public void SetTokenInSession(string token)
        {
            HttpContext.Session.SetString("token", "test");
        }

        [NonAction]
        public async Task<ActionResult<int>> CheckUser(string openId)
        {
            int userId = 0;
            var oaUserList = await _db.oAUser.Where(u => (u.original_id.Trim().Equals(_settings.originalId.Trim())
                && u.open_id.Trim().Equals(openId.Trim()))).ToListAsync();
            if (oaUserList.Count == 0)
            {
                OfficialAccountApi oaApi = new OfficialAccountApi(_db, _config);
                string unionId = oaApi.GetUnionId(openId.Trim()).Value.Trim();
                if (unionId.Trim().Equals(""))
                {
                    return 0;
                }
                var userList = await _db.user.Where(u => u.oa_union_id.Trim().Equals(unionId)).ToListAsync();
                if (userList.Count == 0)
                {
                    User user = new User()
                    {
                        id = 0,
                        oa_union_id = unionId.Trim()
                    };
                    await _db.user.AddAsync(user);
                    await _db.SaveChangesAsync();

                    userId = user.id;
                }
                else
                {
                    userId = userList[0].id;
                }
                if (userId == 0)
                {
                    return 0;
                }
                OAUser oaUser = new OAUser()
                {
                    id = 0,
                    user_id = userId,
                    open_id = openId.Trim(),
                    original_id = _settings.originalId.Trim()
                };
                await _db.oAUser.AddAsync(oaUser);
                await _db.SaveChangesAsync();
                if (oaUser.id == 0)
                {
                    return 0;
                }
            }
            else
            {
                userId = oaUserList[0].user_id;
            }

            return userId;
        }
    }
}
