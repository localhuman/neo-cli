using Microsoft.AspNetCore.Mvc;
using System;
using Neo.Ledger;

namespace Neo.Notifications
{



    #region snippet_ControllerSignature
    [Route("v1/notifications/block")]
    public class BlocksController : ControllerBase
    #endregion
    {


        #region snippet_GetByHeight
        [HttpGet("{height}")]
        [ProducesResponseType(typeof(NotificationResult), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetByHeight(int height, NotificationQuery pageQuery)
        {
            uint blockHeight = Convert.ToUInt32(height);

            if (blockHeight > Blockchain.Singleton.Height)
            {
                return NotFound();
            }

            NotificationResult result = NotificationDB.Instance.NotificationsForBlock(blockHeight, pageQuery);

            result.Paginate(pageQuery);

            return Ok(result);
        }
        #endregion

    }
}