using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OA;
using OA.Models;

namespace OA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelFollowController : ControllerBase
    {
        private readonly SqlServerContext _db;

        private readonly IConfiguration _config;

        private readonly Settings _settings;

        public ChannelFollowController(SqlServerContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _settings = Settings.GetSettings(_config);
        }

        [NonAction]
        public async Task<ChannelFollow?> SetFollow(int userId, int channelUserId)
        {
            var followedUserList = await _db.channelFollow
                .Where(f => ( f.user_id == userId && f.scene.Trim().Equals("follow")))
                .AsNoTracking().ToListAsync();
            if (followedUserList == null || followedUserList.Count == 0)
            {
                ChannelFollow f = new ChannelFollow()
                {
                    id = 0,
                    scene = "follow",
                    user_id = userId,
                    channel_user_id = channelUserId

                };
                await _db.channelFollow.AddAsync(f);
                await _db.SaveChangesAsync();
                return f;
            }
            else
            {
                return null;
            }
        }

        /*
        // GET: api/ChannelFollow
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChannelFollow>>> GetchannelFollow()
        {
          if (_context.channelFollow == null)
          {
              return NotFound();
          }
            return await _context.channelFollow.ToListAsync();
        }

        // GET: api/ChannelFollow/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ChannelFollow>> GetChannelFollow(int id)
        {
          if (_context.channelFollow == null)
          {
              return NotFound();
          }
            var channelFollow = await _context.channelFollow.FindAsync(id);

            if (channelFollow == null)
            {
                return NotFound();
            }

            return channelFollow;
        }

        // PUT: api/ChannelFollow/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChannelFollow(int id, ChannelFollow channelFollow)
        {
            if (id != channelFollow.id)
            {
                return BadRequest();
            }

            _context.Entry(channelFollow).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChannelFollowExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ChannelFollow
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ChannelFollow>> PostChannelFollow(ChannelFollow channelFollow)
        {
          if (_context.channelFollow == null)
          {
              return Problem("Entity set 'SqlServerContext.channelFollow'  is null.");
          }
            _context.channelFollow.Add(channelFollow);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChannelFollow", new { id = channelFollow.id }, channelFollow);
        }

        // DELETE: api/ChannelFollow/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChannelFollow(int id)
        {
            if (_context.channelFollow == null)
            {
                return NotFound();
            }
            var channelFollow = await _context.channelFollow.FindAsync(id);
            if (channelFollow == null)
            {
                return NotFound();
            }

            _context.channelFollow.Remove(channelFollow);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        */

        private bool ChannelFollowExists(int id)
        {
            return (_db.channelFollow?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}
