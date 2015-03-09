<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:wcs="http://schemas.datacontract.org/2004/07/Microsoft.GFS.WCS.Test.Framework"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
  <xsl:template match="wcs:ResultOfTestBatch">
    <html>
      <head><title>Results for Batch:<xsl:value-of select="wcs:Name"/></title></head>
      <body>
        <br>
          Batch State: <b><xsl:value-of select="wcs:BatchState"/>
          </b>
        </br>
        <br>
          Start Time: <b>
          <xsl:value-of select="wcs:BatchStartTime"/>
        </b>
        </br>
        <br>
          CM:<b><xsl:value-of select="wcs:ChassisManagerEndPoint"/></b>
        </br>
        <table border="1">
          <tr>
            <th>REST Uri</th>
            <th>State</th>
            <th>Iterations</th>
            <th>Failure</th>
            <th>Error Message</th>
            <th>Avg Time</th>
          </tr>
          <xsl:for-each select="//wcs:ResultOfTest">
            <tr>
              <td>
                <xsl:value-of select="wcs:RestUri"/>
              </td>
              <td>
                <xsl:value-of select="wcs:State"/>
              </td>
              <td>
                <xsl:value-of select="wcs:IterationsExecutedSuccessfully"/>
              </td>
              <td>
                <xsl:if test="not(wcs:FailedResponseStatusCode/@i:nil)">
                  <xsl:value-of select="wcs:FailedResponseStatusCode"/>
                </xsl:if>
                <xsl:if test="(wcs:FailedResponseStatusCode/@i:nil)">
                  -
                </xsl:if>
              </td>
              <td>
                <xsl:if test="not(wcs:ErrorMessage/@i:nil)">
                  <xsl:value-of select="wcs:ErrorMessage"/>
                </xsl:if>
                <xsl:if test="(wcs:ErrorMessage/@i:nil)">
                  -
                </xsl:if>
              </td>
              <td>
                <xsl:value-of select="wcs:AverageExecutionTime"/>
              </td>
            </tr>
          </xsl:for-each>
        </table>

      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
