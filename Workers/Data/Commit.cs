using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace CIServerBlazor.Workers.Data
{
	public class Commit
	{
		public string Sha { get; set; }

		public string Author { get; set; }
		public string AuthorEmail { get; set; }

		public string FullMessage { get; set; }
		public string ShortMessage { get; set; }

		public List<string> FilesInfo { get; set; }
		public List<Commit> Parents { get; set; }
		public bool BuildResult { get; set; }

		public DateTime Time { get; set; }

		[BsonCtor]
		public Commit()
		{

		}

		public Commit(LibGit2Sharp.Commit commit)
		{
			Sha = commit.Sha;
			Author = commit.Author.Name;
			AuthorEmail = commit.Author.Email;
			Time = commit.Author.When.DateTime;
			FullMessage = commit.Message;
			ShortMessage = commit.MessageShort;
			FilesInfo = new List<string>()
			{

			};
			foreach (var file in commit.Tree)
			{
				FilesInfo.Add($"{file.Path}");
			}

			Parents = new List<Commit>()
			{

			};
			foreach (var parent in commit.Parents)
			{
				Parents.Add(new Commit(parent));
			}
		}
	}
}
