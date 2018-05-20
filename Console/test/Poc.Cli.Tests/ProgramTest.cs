using System;
using Xunit;
using Poc.Cli;

namespace Poc.Cli.Tests
{
    public class ProgramTest
    {
        
        [Fact]
        public void Should_exit_0_When_Any_Args()
        {
            var cliResult = Program.Main(null);

            Assert.Equal(0, cliResult);
        }

        [Fact]
        public void Should_exit_1_When_Any_Args()
        {
            var cliResult = Program.Main(new []{"some args"});

            Assert.Equal(1, cliResult);
        }
    }
}
