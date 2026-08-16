<#
.SYNOPSIS
    Overlay artwork bounds (green) and clickable collider bounds (red) on the
    live game's inventory book page or Block Probability tablet page.

.DESCRIPTION
    Drives a running Ultimate Chicken Horse through the UltimateGlorpExplorer
    bridge. Nothing is installed and nothing persists: the overlay is a set of
    throwaway LineRenderers named DBG_*, removed by -Clear or by the next run.

    This exists because "the icon looks fine" and "you can click the icon" are
    different claims, and only the second one matters to a player. It found
    that Acid's and RCReceiver's hitboxes had drifted entirely off their
    artwork while both looked correct on screen.

.PARAMETER Target
    book    — the inventory book's currently open page (default)
    tablet  — the Block Probability grid's current page

.PARAMETER Clear
    Remove the overlay and exit.

.PARAMETER Shot
    Also write a screenshot to this path.

.EXAMPLE
    tools\draw-bounds.ps1 -Target book -Shot out.png
    tools\draw-bounds.ps1 -Clear
#>
[CmdletBinding()]
param(
    [ValidateSet('book', 'tablet')] [string] $Target = 'book',
    [switch] $Clear,
    [string] $Shot,
    [string] $Bridge = 'http://127.0.0.1:7311'
)

$ErrorActionPreference = 'Stop'

function Invoke-Game([string] $Code) {
    $f = Join-Path $env:TEMP ("uch_bounds_" + [guid]::NewGuid().ToString('N') + ".cs")
    Set-Content -LiteralPath $f -Value $Code -Encoding utf8
    try {
        $r = & curl.exe -sS --max-time 30 -X POST --data-binary "@$f" "$Bridge/execute" | ConvertFrom-Json
    } finally {
        Remove-Item $f -Force -ErrorAction SilentlyContinue
    }
    if (-not $r.ok) { throw "bridge: $($r.error)" }
    return $r.result
}

# Shared preamble. Two things here are not obvious and cost a debugging cycle
# each if you rewrite this by hand:
#
#  * layer 5 (UI). The book and tablet render through InventoryBook.UiCamera,
#    whose culling mask does not include the default layer — lines drawn on
#    layer 0 exist, report no error, and are simply never rasterised.
#  * alpha. A renderer can be enabled, carry a sprite, contribute to bounds and
#    still draw nothing. The glue rig's StickingBlock and RotatingBlock sit at
#    colour alpha 0, and counting them inflated Acid's measured height from
#    0.45 to 2.91.
$preamble = @'
System.Action clear = () => {
    foreach (var old in UnityEngine.Object.FindObjectsOfType<LineRenderer>())
        if (old.gameObject.name.StartsWith("DBG_")) UnityEngine.Object.DestroyImmediate(old.gameObject);
};
var shader = Shader.Find("Sprites/Default");
System.Action<Bounds,Color,string,int> box = (b, col, nm, order) => {
    var go = new GameObject("DBG_" + nm);
    go.layer = 5;
    var lr = go.AddComponent<LineRenderer>();
    lr.useWorldSpace = true; lr.loop = true; lr.positionCount = 4;
    lr.startWidth = 0.04f; lr.endWidth = 0.04f;
    var mat = new Material(shader); mat.color = col;
    lr.material = mat; lr.startColor = col; lr.endColor = col;
    lr.sortingLayerName = "Default"; lr.sortingOrder = order;
    float z = -1.5f;
    lr.SetPosition(0, new Vector3(b.min.x, b.min.y, z));
    lr.SetPosition(1, new Vector3(b.max.x, b.min.y, z));
    lr.SetPosition(2, new Vector3(b.max.x, b.max.y, z));
    lr.SetPosition(3, new Vector3(b.min.x, b.max.y, z));
};
System.Func<Transform,SpriteRenderer,Bounds> visible = (root, skip) => {
    bool any = false; Bounds b = new Bounds();
    foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true)) {
        if (sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy || sr.color.a <= 0.01f) continue;
        if (skip != null && sr.gameObject == skip.gameObject) continue;
        if (!any) { b = sr.bounds; any = true; } else b.Encapsulate(sr.bounds);
    }
    if (!any) b = new Bounds(Vector3.zero, Vector3.zero);
    return b;
};
'@

if ($Clear) {
    Invoke-Game ($preamble + "`nclear();`n`"cleared`"")
    Write-Host 'overlay cleared'
    return
}

if ($Target -eq 'book') {
    $body = @'
clear();
var book = UnityEngine.Object.FindObjectOfType<InventoryBook>();
var page = book.InventoryPages[book.currentPage];
var items = page.transform.Find("Items");
int nv = 0, nc = 0;
var sb = new System.Text.StringBuilder();
sb.AppendLine("page " + page.name);
foreach (Transform c in items) {
    Bounds vis = visible(c, null);
    if (vis.size.sqrMagnitude > 0f) { box(vis, Color.green, "vis_" + c.name, 200); nv++; }
    var pb = c.GetComponent<PickableBlock>();
    if (pb == null || pb.PickColliders == null) continue;
    foreach (var cl in pb.PickColliders) {
        if (cl == null || !cl.enabled) continue;
        box(cl.bounds, Color.red, "hit_" + c.name, 201); nc++;
        Vector3 d = cl.bounds.center - vis.center;
        sb.AppendLine("  " + c.name.PadRight(22)
          + " art " + vis.size.x.ToString("0.##") + "x" + vis.size.y.ToString("0.##")
          + "  hit " + cl.bounds.size.x.ToString("0.##") + "x" + cl.bounds.size.y.ToString("0.##")
          + "  offset " + d.x.ToString("0.##") + "," + d.y.ToString("0.##"));
    }
}
sb.AppendLine("green(art)=" + nv + " red(hit)=" + nc);
sb.ToString()
'@
} else {
    $body = @'
clear();
var book = UnityEngine.Object.FindObjectOfType<InventoryBook>();
var list = book.TabletPage.GetComponent<Tablet>().rulesScreen.tabletBlockList;
int nv = 0, nc = 0;
var sb = new System.Text.StringBuilder();
foreach (var tb in list.tabletBlocks) {
    if (tb == null || tb.spriteHolder == null || tb.spriteHolder.childCount == 0) continue;
    if (!tb.gameObject.activeInHierarchy) continue;
    Bounds vis = visible(tb.spriteHolder.GetChild(0), tb.crossOut);
    if (vis.size.sqrMagnitude > 0f) { box(vis, Color.green, "vis_" + tb.name, 200); nv++; }
    var rt = tb.clickAreaRect;
    if (rt == null) continue;
    Vector3[] cor = new Vector3[4]; rt.GetWorldCorners(cor);
    Bounds hit = new Bounds(cor[0], Vector3.zero);
    for (int i = 1; i < 4; i++) hit.Encapsulate(cor[i]);
    box(hit, Color.red, "hit_" + tb.name, 201); nc++;
}
sb.AppendLine("green(art)=" + nv + " red(hit)=" + nc);
sb.ToString()
'@
}

Invoke-Game ($preamble + "`n" + $body)

if ($Shot) {
    Start-Sleep -Milliseconds 800
    & curl.exe -sS --max-time 30 -o $Shot "$Bridge/screenshot?max=1400" | Out-Null
    Write-Host "screenshot: $Shot"
}
