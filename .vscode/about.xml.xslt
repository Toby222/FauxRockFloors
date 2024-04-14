<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="xml"
                encoding="utf-8"
                indent="yes" />
    <xsl:strip-space elements="*" />
    <!-- Match ModMetaData and its descendants, modify modVersion element -->
    <xsl:template match="/Project/PropertyGroup/ModMetaData|ModMetaData/@*|ModMetaData//node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()" />
        </xsl:copy>
    </xsl:template>
    <!-- Modify modVersion element using the value of VersionPrefix -->
    <xsl:template match="ModMetaData/modVersion">
        <modVersion>
            <xsl:copy-of select="@*" />
            <xsl:value-of select="/Project/PropertyGroup/VersionPrefix" />
        </modVersion>
    </xsl:template>
    <!-- Remove other elements and attributes -->
    <xsl:template match="@*|node()">
        <xsl:apply-templates />
    </xsl:template>
</xsl:stylesheet>