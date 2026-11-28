using Microsoft.Extensions.DependencyInjection;
using RepyPharma.Services.Abstractions;
using Xunit;
namespace RepyPharma.Consumption.Tests;
public class LoadingOperationTests
{
    [Fact]
    public void StateIsScopedAndConcurrentStartsAreRejected()
    {
        using var services=new ServiceCollection().AddScoped<LoadingOperationState>().BuildServiceProvider();
        using var one=services.CreateScope();using var two=services.CreateScope();
        var first=one.ServiceProvider.GetRequiredService<LoadingOperationState>();var second=two.ServiceProvider.GetRequiredService<LoadingOperationState>();
        using var lease=first.TryBegin("Lendo...");Assert.NotNull(lease);Assert.True(first.IsProcessing);Assert.False(second.IsProcessing);Assert.Null(first.TryBegin("Outra"));
        lease.Report("Gravando...");Assert.Equal("Gravando...",first.Message);
    }
    [Fact]
    public void ExceptionReleasesStateAndLateProgressCannotChangeNextOperation()
    {
        var state=new LoadingOperationState();LoadingOperationState.Operation? old=null;
        Assert.Throws<InvalidOperationException>((Action)(()=>{using var operation=state.TryBegin("Lendo");old=operation;throw new InvalidOperationException();}));
        Assert.False(state.IsProcessing);using var next=state.TryBegin("Novo");old!.Report("Antigo");Assert.Equal("Novo",state.Message);
        next!.Dispose();Assert.False(state.IsProcessing);
    }
}
