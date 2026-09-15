using System.Linq;
using Hi_Trade.Services;
using Xunit;

namespace Hi_Trade.Tests;

public class SpacenetScraperTests
{
    [Fact]
    public void ParseCategories_EmptyHtml_ReturnsEmpty()
    {
        var result = SpacenetScraperService.ParseCategories(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseCategories_NoMenuNode_ReturnsEmpty()
    {
        var html = "<html><body><div id=\"other-menu\"><a href=\"/test\">Test</a></div></body></html>";
        var result = SpacenetScraperService.ParseCategories(html);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseCategories_ExtractsHierarchyAndCleansWhitespace()
    {
        var html = """
            <div id="sp-vermegamenu" class="sp-vermegamenu clearfix">
                <ul class="nav navbar-nav menu sp_lesp level-1">
                    <li class="item-1 vertical-cat parent group" data-subwidth="100">
                        <a href="/4-informatique" title="Informatique">
                            <i class="fa fa-icon-1"></i>Informatique
                        </a>
                        <div class="dropdown-menu">
                            <ul class="level-2">
                                <li class="item-2 col-lg-3 cat-child parent">
                                    <a href="/18-ordinateur-portable" title="Ordinateur portable">
                                        <span class="sp_megamenu_title">Ordinateur
                                            portable</span>
                                    </a>
                                    <div class="dropdown-menu">
                                        <ul class="level-3">
                                            <li class="item-3 group">
                                                <a href="/74-pc-portable-tunisie" title="PC Portable">
                                                    <span class="sp_megamenu_title">PC
                                                        Portable</span>
                                                </a>
                                            </li>
                                            <li class="item-3 group">
                                                <a href="/204-pc-portable-gamer-tunisie" title="Pc Portable Gamer">
                                                    <span class="sp_megamenu_title">Pc Portable Gamer</span>
                                                </a>
                                            </li>
                                        </ul>
                                    </div>
                                </li>
                                <li class="item-2 col-lg-3 cat-child">
                                    <a href="/1125-bricolage" title="Bricolage">
                                        <span class="sp_megamenu_title"></span>Bricolage
                                    </a>
                                </li>
                            </ul>
                        </div>
                    </li>
                </ul>
            </div>
            """;

        var categories = SpacenetScraperService.ParseCategories(html).ToList();

        Assert.Equal(5, categories.Count);

        // Level 1
        var l1 = categories.Single(c => c.Url == "https://spacenet.tn/4-informatique");
        Assert.Equal(string.Empty, l1.ParentCategory);
        Assert.Equal("Informatique", l1.Title);

        // Level 2 with children
        var l2A = categories.Single(c => c.Url == "https://spacenet.tn/18-ordinateur-portable");
        Assert.Equal("Informatique", l2A.ParentCategory);
        Assert.Equal("Ordinateur portable", l2A.Title);

        // Level 3 items
        var l3A = categories.Single(c => c.Url == "https://spacenet.tn/74-pc-portable-tunisie");
        Assert.Equal("Ordinateur portable", l3A.ParentCategory);
        Assert.Equal("PC Portable", l3A.Title);

        var l3B = categories.Single(c => c.Url == "https://spacenet.tn/204-pc-portable-gamer-tunisie");
        Assert.Equal("Ordinateur portable", l3B.ParentCategory);
        Assert.Equal("Pc Portable Gamer", l3B.Title);

        // Level 2 without children, title from attribute when span is empty
        var l2B = categories.Single(c => c.Url == "https://spacenet.tn/1125-bricolage");
        Assert.Equal("Informatique", l2B.ParentCategory);
        Assert.Equal("Bricolage", l2B.Title);
    }

    [Fact]
    public void ParseCategories_DeduplicatesUrls_CaseInsensitively()
    {
        var html = """
            <div id="sp-vermegamenu">
                <ul class="level-1">
                    <li class="item-1">
                        <a href="/4-informatique" title="Informatique">Informatique</a>
                        <div class="dropdown-menu">
                            <ul class="level-2">
                                <li class="item-2">
                                    <a href="/18-ordinateurs" title="Ordinateurs">Ordinateurs</a>
                                    <div class="dropdown-menu">
                                        <ul class="level-3">
                                            <li class="item-3">
                                                <a href="/204-pc-gamer" title="PC Gamer 1">PC Gamer 1</a>
                                            </li>
                                        </ul>
                                    </div>
                                </li>
                            </ul>
                        </div>
                    </li>
                    <li class="item-1">
                        <a href="/44-gaming" title="Gaming">Gaming</a>
                        <div class="dropdown-menu">
                            <ul class="level-2">
                                <li class="item-2">
                                    <a href="/203-pc-gaming" title="PC Gaming">PC Gaming</a>
                                    <div class="dropdown-menu">
                                        <ul class="level-3">
                                            <li class="item-3">
                                                <a href="/204-PC-GAMER" title="PC Gamer Duplicate">PC Gamer Duplicate</a>
                                            </li>
                                        </ul>
                                    </div>
                                </li>
                            </ul>
                        </div>
                    </li>
                </ul>
            </div>
            """;

        var categories = SpacenetScraperService.ParseCategories(html).ToList();

        // Should deduplicate /204-pc-gamer vs /204-PC-GAMER
        Assert.Single(categories.Where(c => c.Url.EndsWith("204-pc-gamer", System.StringComparison.OrdinalIgnoreCase)));
        var gamerCat = categories.First(c => c.Url.EndsWith("204-pc-gamer", System.StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Ordinateurs", gamerCat.ParentCategory);
        Assert.Equal("PC Gamer 1", gamerCat.Title);
    }

    [Fact]
    public void ParseCategories_IgnoresInvalidUrls()
    {
        var html = """
            <div id="sp-vermegamenu">
                <ul class="level-1">
                    <li class="item-1">
                        <a href="#" title="Invalid Hash">Invalid Hash</a>
                        <div class="dropdown-menu">
                            <ul class="level-2">
                                <li class="item-2">
                                    <a href="javascript:void(0)" title="Invalid JS">Invalid JS</a>
                                </li>
                                <li class="item-2">
                                    <a href="" title="Empty URL">Empty URL</a>
                                </li>
                                <li class="item-2">
                                    <a href="/valid-cat" title="Valid Category">Valid Category</a>
                                </li>
                            </ul>
                        </div>
                    </li>
                </ul>
            </div>
            """;

        var categories = SpacenetScraperService.ParseCategories(html).ToList();

        Assert.Single(categories);
        Assert.Equal("https://spacenet.tn/valid-cat", categories[0].Url);
        Assert.Equal("Valid Category", categories[0].Title);
    }
}
