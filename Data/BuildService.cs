using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CIServerBlazor.Workers;
using LiteDB;

namespace CIServerBlazor.Data
{
	public class BuildService
	{
		public static LiteDatabase Database;
		private List<GitWorker> Workers;

		public BuildService()
		{
			Workers = new List<GitWorker>();
			if (Database == null)
			{
				Database = new LiteDatabase(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar +
				                            "Database.db");
			}
		}

		public static BuildService GetService()
		{
			return (BuildService)Startup.serviceProvider.GetService(typeof(BuildService));
		}

		public Task<GitWorker[]> GetBuildsHistoryAsync()
		{
			var collections = Database.GetCollection<GitWorker>("builds_history");

			return Task.FromResult(collections.FindAll().OrderByDescending(x => x.StartTime).ToArray());
		}

		public GitWorker GetWorkerById(string id)
		{
			var gitWorker = Workers.FirstOrDefault(x => x.Id == id);
			if (gitWorker == null)
			{
				var collection = Database.GetCollection<GitWorker>("builds_history");
				gitWorker = collection.FindOne(x => x.Id == id);
			}
			return gitWorker;
		}

		public void AddWorker(string gitUrl, string branch, string buildCmd)
		{
			var worker = new GitWorker(gitUrl, branch, buildCmd);
			Workers.Add(worker);
			worker.Start();
		}
	}
}
