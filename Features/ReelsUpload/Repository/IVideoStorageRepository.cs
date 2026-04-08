using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Repository
{
    public interface IVideoStorageRepository
    {
        /// <summary>
        /// Inserts a video file into the database, and returns the stored ReelModel.
        /// Owner: Alex
        /// </summary>
        Task<ReelModel> InsertReelAsync(ReelModel reel);
    }
}
