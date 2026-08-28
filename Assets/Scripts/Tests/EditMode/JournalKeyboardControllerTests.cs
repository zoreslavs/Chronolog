using Chronolog.Presentation;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace Chronolog.Tests
{
    public sealed class JournalKeyboardControllerTests
    {
        [TestCase(true, "", false)]
        [TestCase(false, "", true)]
        [TestCase(false, "A calm afternoon walk.", false)]
        public void ShouldShowPlaceholder_OnlyForAnUnfocusedEmptyField(bool isFocused, string text, bool expected)
        {
            var method = typeof(JournalKeyboardController).GetMethod(
                "ShouldShowPlaceholder",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            var isVisible = (bool)method.Invoke(null, new object[] { isFocused, text });

            Assert.That(isVisible, Is.EqualTo(expected));
        }

        [Test]
        public void GetScreenPosition_MovesTheEntireFormAboveTheKeyboard()
        {
            var method = typeof(JournalKeyboardController).GetMethod(
                "GetScreenPosition",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            var position = (Vector2)method.Invoke(
                null,
                new object[] { new Vector2(0f, -100f), 600f, 2f });

            Assert.That(position, Is.EqualTo(new Vector2(0f, 200f)));
        }
    }
}
