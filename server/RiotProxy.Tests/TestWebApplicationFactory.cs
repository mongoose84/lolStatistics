using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.Security;
using RiotProxy.Infrastructure.Email;
using RiotProxy.External.Domain.Entities;
using Microsoft.Extensions.Hosting;

namespace RiotProxy.Tests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> _overrides;
    private readonly FakeUsersRepository _usersRepository;
    private readonly FakeVerificationTokensRepository _tokensRepository;
    private readonly FakeEmailService _emailService;

    public FakeUsersRepository UsersRepository => _usersRepository;
    public FakeVerificationTokensRepository TokensRepository => _tokensRepository;
    public FakeEmailService EmailService => _emailService;

    public TestWebApplicationFactory(IDictionary<string, string?>? overrides = null)
    {
        _overrides = overrides ?? new Dictionary<string, string?>();
        _usersRepository = new FakeUsersRepository();
        _tokensRepository = new FakeVerificationTokensRepository();
        _emailService = new FakeEmailService();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure process environment reflects testing to allow Secrets.Initialize reinitialization
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Test encryption key (32 bytes base64-encoded) - only for testing
            // "test-encryption-key-32bytes!!!!!" (32 chars) -> base64
            const string testEmailEncryptionKey = "dGVzdC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISEhISE=";

            var defaults = new Dictionary<string, string?>
            {
                ["Auth:EnableMvpLogin"] = "true",
                ["Auth:CookieName"] = "mongoose-auth",
                ["Auth:SessionTimeout"] = "30",
                ["Jobs:EnableMatchHistorySync"] = "false",
                ["RIOT_API_KEY"] = "test-key",
                ["Database_test"] = "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;",
                ["Security:EmailEncryptionKey"] = testEmailEncryptionKey
            };

            config.AddInMemoryCollection(defaults);
            if (_overrides.Count > 0)
            {
                config.AddInMemoryCollection(_overrides);
            }
        });

        builder.ConfigureServices(services =>
        {
            // Replace UsersRepository with a fake to avoid real DB connections
            services.RemoveAll<UsersRepository>();
            services.AddSingleton<UsersRepository>(_usersRepository);

            // Replace VerificationTokensRepository with a fake
            services.RemoveAll<VerificationTokensRepository>();
            services.AddSingleton<VerificationTokensRepository>(_tokensRepository);

            // Replace IEmailService with a fake
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(_emailService);
        });

        return base.CreateHost(builder);
    }

    internal sealed class FakeUsersRepository : UsersRepository
    {
        private readonly ConcurrentDictionary<string, User> _usersByUsername = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<long, User> _usersById = new();
        private long _nextId = 1;

        public FakeUsersRepository() : base(null!, new FakeEncryptor())
        {
            // Pre-populate with a test user (password: "test-password")
            var testUser = new User
            {
                UserId = _nextId++,
                Username = "tester",
                Email = "tester@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test-password"),
                EmailVerified = true,
                IsActive = true,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername["tester"] = testUser;
            _usersByEmail["tester@test.com"] = testUser;
            _usersById[testUser.UserId] = testUser;
        }

        public override Task<User?> GetByUsernameAsync(string username)
        {
            _usersByUsername.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }

        public override Task<User?> GetByEmailAsync(string email)
        {
            _usersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public override Task<long> UpsertAsync(User user)
        {
            if (user.UserId == 0)
            {
                user.UserId = _nextId++;
            }
            _usersByUsername[user.Username] = user;
            _usersByEmail[user.Email] = user;
            _usersById[user.UserId] = user;
            return Task.FromResult(user.UserId);
        }

        public override Task<User?> GetByIdAsync(long userId)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public override Task UpdateEmailVerifiedAsync(long userId, bool verified)
        {
            if (_usersById.TryGetValue(userId, out var user))
            {
                user.EmailVerified = verified;
            }
            return Task.CompletedTask;
        }

        public void AddUnverifiedUser(string username, string email, string password)
        {
            var user = new User
            {
                UserId = _nextId++,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                EmailVerified = false,
                IsActive = true,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername[username] = user;
            _usersByEmail[email] = user;
            _usersById[user.UserId] = user;
        }
    }

    /// <summary>
    /// Fake encryptor for testing that doesn't actually encrypt.
    /// Just passes through the value as-is (or with a simple marker).
    /// </summary>
    private sealed class FakeEncryptor : IEncryptor
    {
        public string Encrypt(string input) => $"encrypted:{input.ToLowerInvariant().Trim()}";
        public string EncryptPreserveCase(string input) => $"encrypted:{input.Trim()}";
        public string Decrypt(string encryptedInput) =>
            encryptedInput.StartsWith("encrypted:")
                ? encryptedInput.Substring("encrypted:".Length)
                : encryptedInput;
    }

    /// <summary>
    /// Fake verification tokens repository for testing.
    /// </summary>
    internal sealed class FakeVerificationTokensRepository : VerificationTokensRepository
    {
        private readonly ConcurrentDictionary<long, VerificationToken> _tokens = new();
        private long _nextId = 1;

        public FakeVerificationTokensRepository() : base(null!) { }

        public override Task<long> CreateTokenAsync(long userId, string tokenType, string code, DateTime expiresAt)
        {
            var token = new VerificationToken
            {
                Id = _nextId++,
                UserId = userId,
                TokenType = tokenType,
                Code = code,
                ExpiresAt = expiresAt,
                UsedAt = null,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _tokens[token.Id] = token;
            return Task.FromResult(token.Id);
        }

        public override Task<VerificationToken?> GetActiveTokenAsync(long userId, string tokenType)
        {
            var token = _tokens.Values
                .Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();
            return Task.FromResult(token);
        }

        public override Task MarkTokenAsUsedAsync(long tokenId)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.UsedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public override Task IncrementAttemptsAsync(long tokenId)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.Attempts++;
            }
            return Task.CompletedTask;
        }

        public override Task<int> CountRecentTokensAsync(long userId, string tokenType, int seconds)
        {
            var since = DateTime.UtcNow.AddSeconds(-seconds);
            var count = _tokens.Values.Count(t => t.UserId == userId && t.TokenType == tokenType && t.CreatedAt > since);
            return Task.FromResult(count);
        }

        public override Task InvalidateActiveTokensAsync(long userId, string tokenType)
        {
            foreach (var token in _tokens.Values.Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null))
            {
                token.UsedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public void AddToken(long userId, string tokenType, string code, DateTime expiresAt)
        {
            var token = new VerificationToken
            {
                Id = _nextId++,
                UserId = userId,
                TokenType = tokenType,
                Code = code,
                ExpiresAt = expiresAt,
                UsedAt = null,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _tokens[token.Id] = token;
        }

        public VerificationToken? GetToken(long tokenId)
        {
            _tokens.TryGetValue(tokenId, out var token);
            return token;
        }

        public IEnumerable<VerificationToken> GetAllTokensForUser(long userId)
        {
            return _tokens.Values.Where(t => t.UserId == userId);
        }
    }

    /// <summary>
    /// Fake email service for testing.
    /// </summary>
    internal sealed class FakeEmailService : IEmailService
    {
        private readonly List<SentEmail> _sentEmails = new();

        public IReadOnlyList<SentEmail> SentEmails => _sentEmails;

        public Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode)
        {
            _sentEmails.Add(new SentEmail(toEmail, username, verificationCode));
            return Task.CompletedTask;
        }

        public void Clear() => _sentEmails.Clear();

        public record SentEmail(string ToEmail, string Username, string VerificationCode);
    }
}
