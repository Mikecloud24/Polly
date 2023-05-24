using Polly.Retry;
using Polly.Strategy;

namespace Polly.Core.Tests.Issues;

public partial class IssuesTests
{
    [Fact]
    public void HandleMultipleResults_898()
    {
        var isRetryKey = new ResiliencePropertyKey<bool>("is-retry");
        var options = new RetryStrategyOptions
        {
            BackoffType = RetryBackoffType.Constant,
            RetryCount = 1,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            ShouldRetry = (outcome, _) => outcome switch
            {
                // handle string results
                { Result: string res } when res == "error" => PredicateResult.True,

                // handle int results
                { Result: int res } when res == -1 => PredicateResult.True,
                _ => PredicateResult.False
            },
            OnRetry = (_, args) =>
            {
                // add a callback updates the resilience context with the retry marker
                args.Context.Properties.Set(isRetryKey, true);
                return default;
            }
        };

        // create the strategy
        var strategy = new ResilienceStrategyBuilder { TimeProvider = TimeProvider.Object }.AddRetry(options).Build();

        // check that int-based results is retried
        bool isRetry = false;
        strategy.Execute(_ =>
        {
            if (isRetry)
            {
                return 0;
            }

            isRetry = true;
            return -1;
        }).Should().Be(0);

        // check that string-based results is retried
        isRetry = false;
        strategy.Execute(_ =>
        {
            if (isRetry)
            {
                return "no-error";
            }

            isRetry = true;
            return "error";
        }).Should().Be("no-error");
    }
}
