namespace Ch23.DocsCheck.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("1. The workflow: GitHub Flow, plus release branches", "1-the-workflow-github-flow-plus-release-branches")]
    [InlineData("Definition of Done (before a story counts as done)", "definition-of-done-before-a-story-counts-as-done")]
    [InlineData("Run `dotnet test`", "run-dotnet-test")]
    [InlineData("Security (Course 2, Chapter 20)", "security-course-2-chapter-20")]
    public void Slug_FollowsGitHubsRules(string heading, string expected) => Assert.Equal(expected, Links.Slug(heading));

    [Fact]
    public void RepeatedHeadings_GetNumberedAnchors() =>
        Assert.Equal(["notes", "notes-1"], Links.Anchors("# Notes\ntext\n## Notes\n").Order());

    [Fact]
    public void HeadingsInsideCodeBlocks_AreNotAnchors() =>
        Assert.Empty(Links.Anchors("```bash\n# not a heading\n```\n"));
}

public class CheckTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("docscheck").FullName;
    public void Dispose() => Directory.Delete(_root, recursive: true);
    private void Write(string rel, string text) { Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(_root, rel))!); File.WriteAllText(Path.Combine(_root, rel), text); }

    [Fact]
    public void GoodLinks_AreFine()
    {
        Write("README.md", "See [setup](docs/setup.md#install) and [the code](src/) and [web](https://example.com/x).\n");
        Write("docs/setup.md", "# Setup\n## Install\n");
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        Assert.Empty(Links.Check(_root));
    }

    [Fact]
    public void AMissingFile_IsReportedWithItsLine()
    {
        Write("README.md", "line one\nSee [old](docs/old.md).\n");
        var p = Assert.Single(Links.Check(_root));
        Assert.Equal(("README.md", 2, "docs/old.md", "no such file"), (p.File, p.Line, p.Target, p.Reason));
    }

    [Fact]
    public void AMissingHeading_IsReported()
    {
        Write("README.md", "See [x](docs/setup.md#configuration).\n");
        Write("docs/setup.md", "# Setup\n## Install\n");
        Assert.Contains("no heading #configuration", Assert.Single(Links.Check(_root)).Reason);
    }

    [Fact]
    public void LinksInCode_AreIgnored()
    {
        Write("README.md", "Use `[x](nowhere.md)` like this.\n```\n[y](nowhere.md)\n```\n");
        Assert.Empty(Links.Check(_root));
    }

    [Fact]
    public void AFolderWithADocscheckIgnoreFile_IsSkipped()
    {
        Write("README.md", "Fine.\n");
        Write("sample/README.md", "Broken on purpose: [x](missing.md).\n");
        Write("sample/.docscheck-ignore", "");
        Assert.Empty(Links.Check(_root));
    }
}
