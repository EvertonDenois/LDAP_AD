using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Text;

namespace DAL.acesso_ad
{
    public class LdapAuthentication
    {
        private string _path;
        private string _filterAttribute;

        public LdapAuthentication(string path)
        {
            _path = path;
        }

        public LdapAuthentication()
        {
            
        }

        public void teste()
        {
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, "embad");
            UserPrincipal user = UserPrincipal.FindByIdentity(ctx, IdentityType.Name, "edenois");

            string email = user.EmailAddress.ToString();
            string chapa = user.EmployeeId.ToString();
            string nome = user.GivenName.ToString();
            string sobrenome = user.Surname.ToString();
            string nomeCompleto = user.DisplayName.ToString(); //.ToLower();
            string ramal = user.VoiceTelephoneNumber.ToString(); 

        }

        /// <summary>
        /// Modelo para busca de dados no AD
        /// </summary>
        public void buscaEmailByNomeCompleto()
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain, Environment.UserDomainName);

            UserPrincipal users = new UserPrincipal(context);
            users.DisplayName = "";

            // create a principal searcher for running a search operation
            PrincipalSearcher pS = new PrincipalSearcher(users);

            // run the query
            PrincipalSearchResult<Principal> results = pS.FindAll();

            foreach (Principal result in results)
            {
                string email = ((UserPrincipal)(result)).EmailAddress;
                // do something useful...
            }
        }

        public bool IsAuthenticated(string domain, string username, string pwd)
        {
            try //Inserido pois alguns Emails não estão no EMBAD e NO PRD
            {
                //string userFullName = UserPrincipal.Current.DisplayName;

                UserPrincipal user = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain, domain), IdentityType.Name, username);
                   
                string email = user.EmailAddress.ToString();
                string chapa = user.EmployeeId.ToString();
                string nome = user.GivenName.ToString();
                string sobrenome = user.Surname.ToString();
                string nomeCompleto = user.DisplayName.ToString(); //.ToLower();
                string ramal = user.VoiceTelephoneNumber.ToString();
            }
            catch (Exception)
            {

            }

            string domainAndUsername = domain + @"\" + username;
            DirectoryEntry entry = new DirectoryEntry(_path, domainAndUsername, pwd);
            try
            {
                //Bind to the native AdsObject to force authentication.
                object obj = entry.NativeObject;

                DirectorySearcher search = new DirectorySearcher(entry);

                search.Filter = "(SAMAccountName=" + username + ")";
                search.PropertiesToLoad.Add("cn");
                search.PropertiesToLoad.Add("mail");
                SearchResult result = search.FindOne();

                if (null == result)
                {
                    return false;
                }

                //Update the new path to the user in the directory.
                _path = result.Path;
                _filterAttribute = (string)result.Properties["cn"][0];
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public string GetGroups()
        {
            DirectorySearcher search = new DirectorySearcher(_path);
            search.Filter = "(cn=" + _filterAttribute + ")";
            search.PropertiesToLoad.Add("memberOf");
            StringBuilder groupNames = new StringBuilder();

            try
            {
                SearchResult result = search.FindOne();
                int propertyCount = result.Properties["memberOf"].Count;
                string dn;
                int equalsIndex, commaIndex;

                for (int propertyCounter = 0; propertyCounter < propertyCount; propertyCounter++)
                {
                    dn = (string)result.Properties["memberOf"][propertyCounter];
                    equalsIndex = dn.IndexOf("=", 1);
                    commaIndex = dn.IndexOf(",", 1);
                    if (-1 == equalsIndex)
                    {
                        return null;
                    }
                    groupNames.Append(dn.Substring((equalsIndex + 1), (commaIndex - equalsIndex) - 1));
                    groupNames.Append("|");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error obtaining group names. " + ex.Message);
            }
            return groupNames.ToString();
        }
    }
}
