// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.SkillDefinition;
using Moq;
using Xunit;

namespace SemanticKernel.UnitTests.Orchestration;

public sealed class SKFunctionTests1
{
    private readonly Mock<IPromptTemplate> _promptTemplate;

    public SKFunctionTests1()
    {
        this._promptTemplate = new Mock<IPromptTemplate>();
        this._promptTemplate.Setup(x => x.RenderAsync(It.IsAny<SKContext>())).ReturnsAsync("foo");
        this._promptTemplate.Setup(x => x.GetParameters()).Returns(new List<ParameterView>());
    }

    [Fact]
    public void ItHasDefaultRequestSettings()
    {
        // Arrange
        var templateConfig = new PromptTemplateConfig();
        var functionConfig = new SemanticFunctionConfig(templateConfig, this._promptTemplate.Object);

        // Act
        var skFunction = SKFunction.FromSemanticConfig("sk", "name", functionConfig);

        // Assert
        Assert.NotNull(skFunction.BackendSettings);
    }

    [Fact]
    public void ItAllowsToUpdateRequestSettings()
    {
        // Arrange
        var templateConfig = new PromptTemplateConfig();
        var functionConfig = new SemanticFunctionConfig(templateConfig, this._promptTemplate.Object);
        var skFunction = SKFunction.FromSemanticConfig("sk", "name", functionConfig);
        var settings = new Dictionary<string, object>
        {
            { "Temperature", 0.9 },
            { "MaxTokens", 2001 }
        };

        // Act
        skFunction.BackendSettings["Temperature"] = 1.3;
        skFunction.BackendSettings["MaxTokens"] = 130;

        // Assert
        Assert.Equal(1.3, skFunction.BackendSettings["Temperature"]);
        Assert.Equal(130, skFunction.BackendSettings["MaxTokens"]);

        // Act
        skFunction.BackendSettings["Temperature"] = 0.7;

        // Assert
        Assert.Equal(0.7, skFunction.BackendSettings["Temperature"]);

        // Act
        skFunction.SetAIConfiguration(settings);

        // Assert
        Assert.Equal(settings["Temperature"], skFunction.BackendSettings["Temperature"]);
        Assert.Equal(settings["MaxTokens"], skFunction.BackendSettings["MaxTokens"]);
    }
}
