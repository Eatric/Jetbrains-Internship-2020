using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CIServerBlazor.Data;
using CIServerBlazor.Pages;
using CIServerBlazor.Workers.Data;
using LibGit2Sharp;
using LiteDB;
using Commit = CIServerBlazor.Workers.Data.Commit;

namespace CIServerBlazor.Workers
{
	public class GitWorker
	{
		[BsonId] public string Id { get; set; } = Guid.NewGuid().ToString();

		public delegate void LogUpdated();

		[BsonIgnore] public event LogUpdated OnLogUpdated;

		public bool IsCommitFounded { get; set; }
		public bool IsSearching { get; set; }
		public bool IsReverted { get; set; }
		public string GitUrl { get; }
		public string BranchName { get; }
		public string BuildCmd { get; }

		[BsonIgnore] private Repository Repository { get; set; }
		public int CountCommits { get; set; }
		private int CurrentCommitId { get; set; }
		public Commit LastCommit { get; set; }
		private Commit CurrentCommit { get; set; }

		private string PathToCurrentCommit { get; set; }
		private DirectoryInfo DirectoryInfo { get; set; }

		public string FullBuildLog { get; set; }
		public DateTime StartTime { get; set; } = DateTime.Now;

		public GitWorker(string gitUrl, string branchName, string buildCmd)
		{
			GitUrl = gitUrl;
			BranchName = branchName;
			BuildCmd = buildCmd;
		}

		private async void CloneRepository()
		{
			await Task.Factory.StartNew(() =>
			{
				PathToCurrentCommit = Guid.NewGuid().ToString();
				DirectoryInfo = Directory.CreateDirectory(PathToCurrentCommit);

				Log("Start Cloning Repository...");
				if (CommandLineExecute("git clone " + GitUrl + " -b " + BranchName + " --progress " +
				                       DirectoryInfo.FullName) != 0)
				{
					Log("Cloning Failed");
					return;
				}
				Log("Successfully Cloned");
				Save();
				Log("Files...");
				foreach (var file in DirectoryInfo.GetFiles())
				{
					Log($"Name: {file.Name} Size: {file.Length} bytes");
				}

				Repository = new Repository(DirectoryInfo.FullName);
			});
		}

		private void Save()
		{
			var collection = BuildService.Database.GetCollection<GitWorker>("builds_history");

			if (collection.Exists(x => x.Id == Id))
			{
				collection.Update(this);
				Console.WriteLine("Updated " + Id);
			}
			else
			{
				collection.Insert(this);
				Console.WriteLine("Inserted " + Id);
			}
		}

		public void Start()
		{
			CloneRepository();
			StartCheck();
		}

		private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.Data)) return;

			Log(e.Data);
		}

		private void CheckoutProgress(string path, int completedsteps, int totalsteps)
		{
			Log($"Checkout: {path} {completedsteps}/{totalsteps}");
		}

		private void Log(string str)
		{
			FullBuildLog += $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}] {str}\n";
			Console.WriteLine(str);

			OnLogUpdated?.Invoke();
		}

		private int CommandLineExecute(string app, string path = null)
		{
			var appName = app.Split()[0];
			var arguments = app.Substring(appName.Length);
			using var proc = new Process
			{
				StartInfo =
					{
						FileName = appName,
						Arguments = arguments,
						UseShellExecute = false,
						CreateNoWindow = false,
						RedirectStandardOutput = true,
						RedirectStandardInput = true,
						RedirectStandardError = true,
						WorkingDirectory = path
					}
			};

			proc.OutputDataReceived += Proc_OutputDataReceived;
			proc.ErrorDataReceived += Proc_OutputDataReceived;

			proc.Start();

			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();

			proc.WaitForExit(6 * 60 * 1000);

			if (!proc.HasExited)
			{
				proc.Kill();
				proc.WaitForExit();
			}

			return proc.ExitCode;
		}

		private void FreeResources()
		{
			try
			{
				Directory.Delete(DirectoryInfo.FullName, true);
			}
			catch (Exception ex)
			{
				// ignored
			}
		}

		private async void StartCheck()
		{
			await Task.Factory.StartNew(() =>
			{
				while (!IsCommitFounded)
				{
					if (Repository == null || !Repository.Branches.Any()) continue;

					IsSearching = true;

					if (CountCommits == 0)
					{
						CountCommits = Repository.Commits.Count();
						CurrentCommitId = 0;
						Log($"Commits to scan {CountCommits}");
					}

					if (Repository.Commits.Count() == 1)
					{
						CurrentCommitId = 0;
					}

					var repCommit = Repository.Commits.ElementAt(CurrentCommitId);
					CurrentCommit = new Commit(repCommit);

					if (LastCommit?.Sha == CurrentCommit.Sha)
					{
						IsSearching = false;
						FreeResources();
						break;
					}

					Log($"Checkout to {CurrentCommit.Sha}");
					Commands.Checkout(Repository, repCommit, new CheckoutOptions()
					{
						OnCheckoutProgress = CheckoutProgress
					});

					Log($"Start build {CurrentCommit.Sha}");

					Save();

					var exitCode = CommandLineExecute(BuildCmd,  DirectoryInfo.FullName);

					if (exitCode == 0 && LastCommit?.BuildResult == false)
					{
						CurrentCommit.BuildResult = true;
						IsCommitFounded = true;
						IsSearching = false;
						Log($"Broken Commit is {LastCommit.Sha} committed by {LastCommit.Author}[{LastCommit.AuthorEmail}]");

						Save();

						var revertCode = CommandLineExecute($"git checkout {BranchName} & git revert {LastCommit.Sha} --no-edit", DirectoryInfo.FullName);
						
						if (revertCode == 0)
						{
							CommandLineExecute("git push", DirectoryInfo.FullName);
							IsReverted = true;
							Log("Commit successfully reverted");
						}
						else
						{
							IsReverted = false;
							Log("Commit revert is failed");
						}

						FreeResources();
						break;
					}

					LastCommit = CurrentCommit;
					CurrentCommitId = 1;
				}
			});
		}
	}
}
