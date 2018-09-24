﻿using CodeHub.Helpers;
using Octokit;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CodeHub.Services
{

	class UserService
	{
		/// <summary>
		/// Gets info of a given user
		/// </summary>
		/// <param name="login"></param>
		/// <returns></returns>
		public static async Task<User> GetUserInfo(string login)
		{
			try
			{
				var user = await GlobalHelper.GithubClient.User.Get(login);
				return user;
			}
			catch
			{
				return null;
			}

		}

		/// <summary>
		/// Follows a given user
		/// </summary>
		/// <param name="login"></param>
		/// <returns></returns>
		public static async Task<bool> FollowUser(string login)
		{
			try
			{
				return await GlobalHelper.GithubClient.User.Followers.Follow(login);
			}
			catch
			{
				return false;
			}
		}
		/// <summary>
		/// Unfollows a given user
		/// </summary>
		/// <param name="login"></param>
		/// <returns></returns>
		public static async Task UnFollowUser(string login) 
			=> await GlobalHelper.GithubClient.User.Followers.Unfollow(login);

		/// <summary>
		/// Checks if the current user follows a given user
		/// </summary>
		/// <param name="login"></param>
		/// <returns></returns>
		public static async Task<bool> CheckFollow(string login) 
			=> await GlobalHelper.GithubClient.User.Followers.IsFollowingForCurrent(login);

		/// <summary>
		/// Gets the authenticated GithubClient
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public static GitHubClient GetAuthenticatedClient(string token) => 
			new GitHubClient(new ProductHeaderValue("CodeHub"))
			{
				Credentials = new Credentials(token)
			};

		/// <summary>
		/// Gets the current user info
		/// </summary>
		/// <returns></returns>
		public static async Task<User> GetCurrentUserInfo()
		{
			try
			{
				var user = await GlobalHelper.GithubClient.User.Current();
				return user;
			}
			catch
			{
				return null;
			}

		}

		/// <summary>
		/// Gets Email of current user
		/// </summary>
		/// <returns></returns>
		public static async Task<string> GetUserEmail()
		{
			try
			{
				var result = await GlobalHelper.GithubClient.User.Email.GetAll();
				var s = result[0].Email.ToString();
				return s;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all repositories of current user
		/// </summary>
		/// <returns></returns>
		public static async Task<ObservableCollection<Repository>> GetUserRepositories()
		{
			try
			{
				var repos = await GlobalHelper.GithubClient.Repository.GetAllForCurrent();
				return new ObservableCollection<Repository>(repos);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all gists of current user
		/// </summary>
		/// <returns></returns>
		public static async Task<ObservableCollection<Gist>> GetUserGists()
		{
			try
			{
				var gists = await GlobalHelper.GithubClient.Gist.GetAll();
				return new ObservableCollection<Gist>(gists);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all public events of current user
		/// </summary>
		/// <returns></returns>
		public static async Task<ObservableCollection<Activity>> GetUserActivity(int pageIndex)
		{
			try
			{
				var options = new ApiOptions
				{
					PageSize = 10,
					PageCount = 1,
					StartPage = pageIndex
				};
				var result = await GlobalHelper.GithubClient.Activity.Events.GetAllUserReceivedPublic(GlobalHelper.UserLogin, options);

				return new ObservableCollection<Activity>(result);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all repositories starred by current user
		/// </summary>
		/// <returns></returns>
		public static async Task<ObservableCollection<Repository>> GetStarredRepositories()
		{
			try
			{
				var repos = await GlobalHelper.GithubClient.Activity.Starring.GetAllForCurrent();
				return new ObservableCollection<Repository>(repos);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all followers of a given user
		/// </summary>
		/// <param name="login"></param>
		/// <returns></returns>
		public static async Task<ObservableCollection<User>> GetAllFollowers(string login)
		{
			try
			{
				var firstOneHundred = new ApiOptions
				{
					PageSize = 100,
					PageCount = 1
				};

				var result = await GlobalHelper.GithubClient.User.Followers.GetAll(login, firstOneHundred);

				return new ObservableCollection<User>(result);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all users a given user is following
		/// </summary>
		/// <param name="login"></param>
		/// <returns></returns>
		public static async Task<ObservableCollection<User>> GetAllFollowing(string login)
		{
			try
			{
				var firstOneHundred = new ApiOptions
				{
					PageSize = 100,
					PageCount = 1
				};
				var result = await GlobalHelper.GithubClient.User.Followers.GetAllFollowing(login, firstOneHundred);

				return new ObservableCollection<User>(result);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all organizations for current user
		/// </summary>
		public static async Task<ObservableCollection<Organization>> GetAllOrganizations()
		{
			try
			{
				var list = await GlobalHelper.GithubClient.Organization.GetAllForCurrent();
				return new ObservableCollection<Organization>(list);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all issues started by the current user for a given repository
		/// </summary>
		/// <param name="repoId"></param>
		/// <returns></returns>
		public static async Task<ObservableCollection<Issue>> GetAllIssuesForRepoByUser(long repoId)
		{
			try
			{
				var issues = await GlobalHelper.GithubClient.Issue.GetAllForRepository(repoId, new RepositoryIssueRequest
				{
					State = ItemStateFilter.All,
					Creator = GlobalHelper.UserLogin

				});

				return new ObservableCollection<Issue>(issues);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Updates a user's profile
		/// </summary>
		/// <param name="userUpdate"></param>
		/// <returns></returns>
		public static async Task<User> UpdateUserProfile(UserUpdate userUpdate)
		{
			try
			{
				return await GlobalHelper.GithubClient.User.Update(userUpdate);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets all email addresses of the current user
		/// </summary>
		/// <returns></returns>
		public static async Task<ObservableCollection<EmailAddress>> GetVerifiedEmails()
		{
			try
			{
				var emails = await GlobalHelper.GithubClient.User.Email.GetAll();
				return new ObservableCollection<EmailAddress>(emails);
			}
			catch
			{
				return null;
			}
		}
	}
}
