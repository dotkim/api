using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Api.ServiceInterface.Storage;
using Api.ServiceModel;
using Api.ServiceModel.Entities;
using ServiceStack;
using ServiceStack.Logging;

namespace Api.ServiceInterface
{
  [Authenticate]
  [RequiredRole("Admin")]
  public class ImageService : Service
  {
    private static ILog _Log = LogManager.GetLogger(typeof(ImageService));

    public async Task<GetImageRandomResponse> GetAsync(GetImageRandom request)
    {
      var query = await Image.GetRandom(request.GuildId, request.Filter);
      if (query is null) throw new FileNotFoundException("There are no image files for this guild.");

      return new GetImageRandomResponse { FileInfo = query };
    }

    public async Task<object> PostAsync(PostImage request)
    {
      List<string> files = FileHandler.Process(base.Request.Files, "image");
      if (files.Count <= 0) throw new ArgumentNullException("Files");

      // Create an int to see if we ignored any files while processing.
      // If a file is ignored it is most likely a wrong format for this endpoint.
      int ignoredFiles = 0;
      if (base.Request.Files.Length != files.Count)
        ignoredFiles = base.Request.Files.Length - files.Count;

      // Create a dict to check for insert or update errors.
      // This is used for possibly returning another code to the user
      // if one or more files wasn't upserted into the database.
      var checkList = new Dictionary<string, bool>();

      foreach (string file in files)
      {
        string ext = file.Split(".").Last();

        var image = new Image();
        image.Name = file;
        image.GuildId = request.GuildId;
        image.UploaderId = request.UploaderId;
        image.Extension = ext;
        image.Tags = new List<string> { "tagme" };

        bool check = await image.Exists();

        bool query;
        // A file should be updated when it already exists.
        // If it doesn't its inserted.
        if (check)
        {
          query = await image.Update();
        }
        else
        {
          query = await image.Insert();
        }

        // Adds the file and query result.
        checkList.Add(file, query);
      }

      if (ignoredFiles > 0) _Log.InfoFormat("Ignored {int} file(s) because of filetype.", ignoredFiles);
      foreach (string key in checkList.Keys)
      {
        if (!checkList[key]) _Log.InfoFormat("{key} was not inserted into the database.", key);
      }

      return HttpResult.Redirect("/");
    }
  }
}
