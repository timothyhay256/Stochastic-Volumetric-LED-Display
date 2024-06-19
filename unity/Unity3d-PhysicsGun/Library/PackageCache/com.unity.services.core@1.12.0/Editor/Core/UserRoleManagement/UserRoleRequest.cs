using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Internal.Serialization;
using UnityEditor;
using UnityEngine.Networking;

namespace Unity.Services.Core.Editor
{
    class UserRoleRequest : IUserRoleRequest
    {
        readonly IJsonSerializer m_Serializer;

        public UserRoleRequest()
            : this(new NewtonsoftSerializer()) {}

        internal UserRoleRequest(IJsonSerializer serializer) => m_Serializer = serializer;

        public IAsyncOperation<UserRole> GetUserRole()
        {
            var resultAsyncOp = new AsyncOperation<UserRole>();
            try
            {
                resultAsyncOp.SetInProgress();
                var cdnEndpoint = new DefaultCdnConfiguredEndpoint();
                var configurationRequestTask = cdnEndpoint.GetConfiguration();
                configurationRequestTask.Completed += configOperation => QueryProjectUsers(configOperation, resultAsyncOp);
            }
            catch (Exception ex)
            {
                resultAsyncOp.Fail(ex);
            }

            return resultAsyncOp;
        }

        void QueryProjectUsers(IAsyncOperation<DefaultCdnEndpointConfiguration> configurationRequestTask, AsyncOperation<UserRole> resultAsyncOp)
        {
            try
            {
#if ENABLE_EDITOR_GAME_SERVICES
                var organizationKey = CloudProjectSettings.organizationKey;
#else
                var organizationKey = CloudProjectSettings.organizationId;
#endif
                var usersUrl = configurationRequestTask.Result.BuildUsersUrl(organizationKey, CloudProjectSettings.projectId);
                var getProjectUsersRequest = new UnityWebRequest(
                    usersUrl,
                    UnityWebRequest.kHttpVerbGET)
                {
                    downloadHandler = new DownloadHandlerBuffer()
                };
                getProjectUsersRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {CloudProjectSettings.accessToken}");
                var operation = getProjectUsersRequest.SendWebRequest();
                operation.completed += op => FindUserRoleToFinishAsyncOperation(getProjectUsersRequest, resultAsyncOp);
            }
            catch (Exception ex)
            {
                resultAsyncOp.Fail(ex);
            }
        }

        void FindUserRoleToFinishAsyncOperation(UnityWebRequest getProjectUsersRequest, AsyncOperation<UserRole> resultAsyncOp)
        {
            const int requestNotAuthorizedCode = 401;
            try
            {
                if (getProjectUsersRequest.responseCode == requestNotAuthorizedCode)
                {
                    throw new RequestNotAuthorizedException();
                }

                UserRole currentUserRole;
                var userList = ExtractUserListFromUnityWebRequest(getProjectUsersRequest);
                if (userList != null)
                {
                    var currentUser = FindCurrentUserInList(CloudProjectSettings.userId, userList.Users);
                    if (currentUser != null)
                    {
                        currentUserRole = currentUser.Role;
                    }
                    else
                    {
                        throw new CurrentUserNotFoundException();
                    }
                }
                else
                {
                    throw new UserListNotFoundException();
                }

                resultAsyncOp.Succeed(currentUserRole);
            }
            catch (Exception ex)
            {
                resultAsyncOp.Fail(ex);
            }
            finally
            {
                getProjectUsersRequest.Dispose();
            }
        }

        UserList ExtractUserListFromUnityWebRequest(UnityWebRequest unityWebRequest)
        {
            if (!UnityWebRequestHelper.IsUnityWebRequestReadyForTextExtract(unityWebRequest, out var jsonContent))
            {
                return null;
            }

            m_Serializer.TryJsonDeserialize<UserList>(jsonContent, out var userList);
            return userList;
        }

        static User FindCurrentUserInList(string currentUserId, IEnumerable<User> users)
        {
            foreach (var user in users)
            {
                if (user.ForeignKey.Equals(currentUserId))
                {
                    return user;
                }
            }

            return null;
        }

        [Serializable]
        class UserList
        {
            [JsonProperty("users")]
            public User[] Users { get; set; }
        }

        [Serializable]
        class User
        {
            [JsonProperty("foreign_key")]
            public string ForeignKey { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("access_granted_by")]
            public string AccessGrantedBy { get; set; }

            [JsonProperty("role")]
            public UserRole Role { get; set; }
        }

        internal class RequestNotAuthorizedException : Exception {}

        class CurrentUserNotFoundException : Exception {}

        class UserListNotFoundException : Exception {}
    }
}
