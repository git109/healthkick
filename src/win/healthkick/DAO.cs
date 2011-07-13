using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SubSonic.Repository;
using SubSonic.Linq;

namespace healthkick
{

  public class DAO
  {
    private SimpleRepository repo;

    public DAO()
    {
      repo = new SimpleRepository(SimpleRepositoryOptions.RunMigrations);
      // find active user
      if (repo.Exists<User>(x => x.Active == true))
      {
        var user = repo.Single<User>(x => x.Active == true);
      }
      else
      {
        // create user
        var user = new User();
        user.ID = Guid.NewGuid();
        // TODO Allow manual entry of username
        user.Name = "Eric";
        user.LastReading = DateTime.Now;
        user.Active = true;
        repo.Add(user);
      }
    }

    public User GetUser()
    {
      var user = repo.Single<User>(x => x.Active == true);
      return user;
    }
  }

  public class User
  {
    public User()
    {
    }

    public Guid ID { get; set; }
    public string Name { get; set; }
    public DateTime LastReading { get; set; }
    public Boolean Active { get; set; }

  }
}
