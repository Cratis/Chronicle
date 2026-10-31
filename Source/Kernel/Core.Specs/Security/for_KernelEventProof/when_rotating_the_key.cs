// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_KernelEventProof;

public class when_rotating_the_key : Specification
{
    ServiceProvider _services;
    IKernelEventProof _proof;
    string _signed;
    bool _verified;

    void Establish()
    {
        var repository = new MemoryRepository();
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("Chronicle");
        services.Configure<KeyManagementOptions>(options => options.XmlRepository = repository);
        _services = services.BuildServiceProvider();
        _proof = new KernelEventProof(_services.GetRequiredService<IDataProtectionProvider>());
        _signed = _proof.Sign(typeof(UserGrantedAdministrator).GetEventType(), "roster", EventSequenceNumber.BeforeFirst, "body");
    }

    void Because()
    {
        var now = DateTimeOffset.UtcNow;
        _services.GetRequiredService<IKeyManager>().CreateNewKey(now, now.AddDays(90));
        _verified = _proof.Verify(typeof(UserGrantedAdministrator).GetEventType(), "roster", EventSequenceNumber.BeforeFirst, "body", _signed);
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_keep_the_old_proof_readable() => _verified.ShouldBeTrue();

    sealed class MemoryRepository : IXmlRepository
    {
        readonly List<XElement> _elements = [];
        public IReadOnlyCollection<XElement> GetAllElements() => _elements.Select(element => new XElement(element)).ToArray();
        public void StoreElement(XElement element, string friendlyName) => _elements.Add(new XElement(element));
    }
}
