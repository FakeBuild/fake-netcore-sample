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
            var cliResult = Program.Main();

            Assert.Equals(0, cliResult);
        }

        [Fact]
        public void Should_exit_1_When_Any_Args()
        {
            var cliResult = Program.Main("some args");

            Assert.Equals(1);
        }
    }
}
