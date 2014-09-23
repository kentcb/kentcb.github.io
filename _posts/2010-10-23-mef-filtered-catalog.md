---
title: MEF Filtered Catalog
assets: /assets/2010-10-23-mef-filtered-catalog/
tags: [ ".NET", "MEF" ]
---
Another piece of MEF code Glenn prompted me to share is a filtered catalog. Here it is in its entirety:

{% highlight C# %}
private abstract class FilteredCatalog : ComposablePartCatalog
{
    private readonly ComposablePartCatalog catalogToFilter;
 
    public FilteredCatalog(ComposablePartCatalog catalogToFilter)
    {
        this.catalogToFilter = catalogToFilter;
    }
 
    public override IQueryable<ComposablePartDefinition> Parts
    {
        get
        {
            return from part in this.catalogToFilter.Parts
                   from exportDefinition in part.ExportDefinitions
                   where this.IsMatch(part) && this.IsMatch(exportDefinition)
                   select part;
        }
    }
 
    public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
    {
        return from export in base.GetExports(definition)
               where this.IsMatch(export.Item1) && this.IsMatch(export.Item2)
               select export;
    }
 
    protected virtual bool IsMatch(ComposablePartDefinition composablePartDefinition)
    {
        return true;
    }
 
    protected virtual bool IsMatch(ExportDefinition exportDefinition)
    {
        return true;
    }
}
{% endhighlight %}

The idea of this abstract class is to make it easy for you to provide filtering logic to pick and choose the parts that are of interest to you. For example, suppose you want a catalog to filter out all services:

{% highlight C# %}
private sealed class NonServiceCatalog : FilteredCatalog
{
    public NonServiceCatalog(ComposablePartCatalog catalogToFilter)
        : base(catalogToFilter)
    {
    }
 
    protected override bool IsMatch(ExportDefinition exportDefinition)
    {
        // in this case, services are identified via some metadata
        return !exportDefinition.Metadata.ContainsKey("Mode");
    }
}
{% endhighlight %}

It is entirely possible that you could alter `FilteredCatalog` such that it is concrete and takes lambdas to filter out parts. However, I prefer the explicit approach because it results in clear code such as:

{% highlight C# %}
rootCatalog.Catalogs.Add(new NonServiceCatalog(catalog));
{% endhighlight %}

Well, that’s it really. A simple, but effective way of filtering your MEF parts.

Enjoy!