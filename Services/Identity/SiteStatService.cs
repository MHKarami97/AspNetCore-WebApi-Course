﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using DNTPersianUtils.Core;
using Entities.Identity;
using Entities.User;
using Services.Contracts.Identity;

namespace Services.Identity
{
    public class SiteStatService : ISiteStatService
    {
        private readonly IUnitOfWork _uow;
        private readonly IApplicationUserManager _userManager;
        private readonly DbSet<User> _users;

        public SiteStatService(
            IApplicationUserManager userManager,
            IUnitOfWork uow)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(_userManager));
            _uow = uow ?? throw new ArgumentNullException(nameof(_uow));
            _users = uow.Set<User>();
        }

        public Task<List<User>> GetOnlineUsersListAsync(int numbersToTake, int minutesToTake)
        {
            var now = DateTime.UtcNow;
            var minutes = now.AddMinutes(-minutesToTake);
            return _users.AsNoTracking()
                         .Where(user => user.LastVisitDateTime != null && user.LastVisitDateTime.Value <= now
                                        && user.LastVisitDateTime.Value >= minutes)
                         .OrderByDescending(user => user.LastVisitDateTime)
                         .Take(numbersToTake)
                         .ToListAsync();
        }

        public Task<List<User>> GetTodayBirthdayListAsync()
        {
            var now = DateTime.UtcNow;
            var day = now.Day;
            var month = now.Month;
            return _users.AsNoTracking()
                         .Where(user => user.Birthday != null && user.IsActive
                                        && user.Birthday.Value.Day == day
                                        && user.Birthday.Value.Month == month)
                         .ToListAsync();
        }

        public async Task<AgeStatViewModel> GetUsersAverageAge()
        {
            var users = await _users.AsNoTracking()
                                    .Where(x => x.Birthday != null && x.IsActive)
                                    .OrderBy(x => x.Birthday)
                                    .ToListAsync();

            var count = users.Count;
            if (count == 0)
            {
                return new AgeStatViewModel();
            }

            var sum = users.Where(user => user.Birthday != null).Sum(user => (int?)user.Birthday.Value.GetAge()) ?? 0;

            return new AgeStatViewModel
            {
                AverageAge = sum / count,
                MaxAgeUser = users.First(),
                MinAgeUser = users.Last(),
                UsersCount = count
            };
        }

        public async Task UpdateUserLastVisitDateTimeAsync(ClaimsPrincipal claimsPrincipal)
        {
            var user = await _userManager.GetUserAsync(claimsPrincipal);
            user.LastVisitDateTime = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
    }
}