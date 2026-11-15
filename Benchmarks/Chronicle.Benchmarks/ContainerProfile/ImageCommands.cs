// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Builds bounded image preparation and offline-inspection commands.</summary>
internal static class ImageCommands
{
    /// <summary>Pulls a measurement dependency before timing begins.</summary>
    /// <param name="image">The image reference.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Pull(string image) => new("pull runtime dependency", ["pull", image], TimeSpan.FromMinutes(5));

    /// <summary>Builds a labelled scratch image.</summary>
    /// <param name="directory">The build context.</param>
    /// <param name="image">The image tag.</param>
    /// <param name="session">The session identity.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Build(string directory, string image, string session) => new("build scratch image", ["build", "-f", Path.Combine(ProfileRepository.Root(), "Docker", "Production", "Dockerfile"), "--label", "cratis.container-profile=1", "--label", $"cratis.container-profile.session={session}", "--tag", image, directory], TimeSpan.FromSeconds(1200));

    /// <summary>Creates, but never starts, a container for PE inspection.</summary>
    /// <param name="image">The image reference.</param>
    /// <param name="name">The container name.</param>
    /// <param name="session">The session identity.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Create(string image, string name, string session) => new("create offline image check", ["create", "--name", name, "--label", "cratis.container-profile=1", "--label", $"cratis.container-profile.session={session}", image], TimeSpan.FromSeconds(60));

    /// <summary>Copies application files from a stopped inspection container.</summary>
    /// <param name="name">The container identity.</param>
    /// <param name="destination">The destination directory.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Copy(string name, string destination) => new("copy image application for PE check", ["cp", $"{name}:/app/.", destination], TimeSpan.FromSeconds(60));
}
