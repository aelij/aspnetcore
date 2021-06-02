// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.JSInterop.Infrastructure
{
    public class ByteArrayJsonConverterTest
    {
        private readonly JSRuntime JSRuntime;
        private JsonSerializerOptions JsonSerializerOptions => JSRuntime.JsonSerializerOptions;

        public ByteArrayJsonConverterTest()
        {
            JSRuntime = new TestJSRuntime();
        }

        [Fact]
        public void Read_Throws_IfByteArraysToBeRevivedIsEmpty()
        {
            // Arrange
            var json = "{}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));
            Assert.Equal("ByteArraysToBeRevived is empty.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonIsMissingByteArraysProperty()
        {
            // Arrange
            JSRuntime.ByteArraysToBeRevived.Append(new byte[] { 1, 2 });

            var json = "{}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));
            Assert.Equal("Required property __byte[] not found.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonContainsUnknownContent()
        {
            // Arrange
            JSRuntime.ByteArraysToBeRevived.Append(new byte[] { 1, 2 });

            var json = "{\"foo\":2}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));
            Assert.Equal("Required property __byte[] not found.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonIsIncomplete()
        {
            // Arrange
            JSRuntime.ByteArraysToBeRevived.Append(new byte[] { 1, 2 });

            var json = $"{{\"__byte[]\":0";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_ReadsJson()
        {
            // Arrange
            var byteArray = new byte[] { 1, 5, 7 };
            JSRuntime.ByteArraysToBeRevived.Append(byteArray);

            var json = $"{{\"__byte[]\":0}}";

            // Act
            var deserialized = JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Equal(byteArray, deserialized);
        }

        [Fact]
        public void Read_IfByteArraysIdAppearsMultipleTimesUseLastProperty()
        {
            // Arrange
            var byteArray = new byte[] { 1, 5, 7 };
            JSRuntime.ByteArraysToBeRevived.Append(byteArray);

            var json = $"{{\"__byte[]\":9120,\"__byte[]\":0}}";

            var deserialized = JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions)!;

            // Act & Assert
            Assert.Equal(byteArray, deserialized);
        }

        [Fact]
        public void Read_ReturnsTheCorrectInstance()
        {
            // Arrange
            // Track a few arrays and verify that the deserialized value returns the correct value.
            var byteArray1 = new byte[] { 1, 5, 7 };
            var byteArray2 = new byte[] { 2, 6, 8 };
            var byteArray3 = new byte[] { 2, 6, 8 };

            JSRuntime.ByteArraysToBeRevived.Append(byteArray1);
            JSRuntime.ByteArraysToBeRevived.Append(byteArray2);
            JSRuntime.ByteArraysToBeRevived.Append(byteArray3);

            var json = $"[{{\"__byte[]\":2}},{{\"__byte[]\":1}}]";

            // Act
            var deserialized = JsonSerializer.Deserialize<byte[][]>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Same(byteArray3, deserialized[0]);
            Assert.Same(byteArray2, deserialized[1]);
        }

        [Fact]
        public void WriteJsonMultipleTimes_IncrementsByteArrayId()
        {
            // Arrange
            var byteArray = new byte[] { 1, 5, 7 };

            // Act & Assert
            for (var i = 0; i < 10; i++)
            {
                var json = JsonSerializer.Serialize(byteArray, JsonSerializerOptions);
                Assert.Equal($"{{\"__byte[]\":{i + 1}}}", json);
            }
        }
    }
}
