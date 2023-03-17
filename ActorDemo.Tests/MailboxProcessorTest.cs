using NUnit.Framework;

namespace ActorDemo.Tests;

[TestFixture]
public class MailboxProcessorTest
{
    [Test]
    public void CanBeInstantiated()
    {
        // Act
        var mailboxProcessor = new MailboxProcessor("name", null, null);

        // Assert
        Assert.IsNotNull(mailboxProcessor);
        Assert.AreEqual("name", mailboxProcessor.Name);
        Assert.IsNull(mailboxProcessor.Parent);
        CollectionAssert.IsEmpty(mailboxProcessor.Children);
        CollectionAssert.IsEmpty(mailboxProcessor._stash);
    }
    
    // SendMessage
    
    // child handling
    
    // stash handling
    
    // tostring
    [Test]
    public void ToString_GeneratesName()
    {
        // Arrange
        var mailboxProcessor = new MailboxProcessor("wx-7", null, null);
        
        // Assert
        Assert.AreEqual("Actor 'wx-7'", mailboxProcessor.ToString());
    }
}