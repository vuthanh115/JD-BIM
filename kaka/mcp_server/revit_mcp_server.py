#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Revit MCP Server (Model Context Protocol)
Connects Antigravity AI Agent with Autodesk Revit via kaka.Structure Add-in.
"""

import os
import sys
import json
import urllib.request
import urllib.error
from typing import Any, Optional, List

from mcp.server.mcpserver import MCPServer

REVIT_MCP_URL = os.getenv("REVIT_MCP_URL", "http://localhost:8080").rstrip("/")

server = MCPServer(
    name="Revit-MCP-Bridge",
    description="Bridge connecting Antigravity AI Agent to Autodesk Revit API (kaka.Structure)",
    version="1.0.0"
)

def _request_revit(endpoint: str, method: str = "GET", payload: Optional[dict] = None) -> dict[str, Any]:
    url = f"{REVIT_MCP_URL}{endpoint}"
    data_bytes = None
    headers = {"Content-Type": "application/json", "Accept": "application/json"}

    if payload is not None:
        data_bytes = json.dumps(payload).encode("utf-8")

    req = urllib.request.Request(url, data=data_bytes, headers=headers, method=method)

    try:
        with urllib.request.urlopen(req, timeout=12) as response:
            res_body = response.read().decode("utf-8")
            return json.loads(res_body)
    except urllib.error.URLError as e:
        return {
            "error": f"Cannot connect to Revit at {url}.",
            "reason": str(e),
            "hint": "Please make sure Revit is running with the kaka.Structure add-in installed and active."
        }
    except Exception as ex:
        return {"error": str(ex)}


@server.tool(
    name="revit_get_status",
    description="Check Revit connection status, add-in health, and active port."
)
def revit_get_status() -> dict[str, Any]:
    """Check Revit connection status."""
    return _request_revit("/api/status")


@server.tool(
    name="revit_get_model_info",
    description="Get metadata of the currently active Revit model: project name, number, file path, active view, and all levels (floors)."
)
def revit_get_model_info() -> dict[str, Any]:
    """Get project info, levels, and active view."""
    return _request_revit("/api/model-info")


@server.tool(
    name="revit_query_elements",
    description="Query and list Revit elements by category (e.g. Columns, Walls, Floors, Beams/Framing, Doors, Windows, Stairs, Foundations, Rebar), level name, and text search."
)
def revit_query_elements(
    category: str = "",
    level: str = "",
    search: str = "",
    limit: int = 50
) -> dict[str, Any]:
    """
    Search elements in the active Revit model.
    Args:
        category: BuiltInCategory or keyword (e.g. 'Columns', 'Walls', 'Floors', 'Framing', 'Doors', 'Windows', 'OST_StructuralColumns').
        level: Name of the level to filter by (e.g. 'Level 1', 'Tang 2').
        search: Substring to match in family name or type name.
        limit: Maximum number of elements to return (default 50, max 200).
    """
    payload = {
        "category": category,
        "level": level,
        "search": search,
        "limit": min(limit, 200)
    }
    return _request_revit("/api/elements/query", method="POST", payload=payload)


@server.tool(
    name="revit_select_elements",
    description="Select and highlight specific elements in Revit by their numeric element IDs, and zoom the active view to fit them."
)
def revit_select_elements(element_ids: List[int], zoom: bool = True) -> dict[str, Any]:
    """
    Select and focus elements in the Revit user interface.
    Args:
        element_ids: List of integer element IDs to select.
        zoom: Whether to zoom the active view to show selected elements.
    """
    payload = {
        "elementIds": element_ids,
        "zoom": zoom
    }
    return _request_revit("/api/elements/select", method="POST", payload=payload)


@server.tool(
    name="revit_set_parameter",
    description="Set or update an instance parameter (Comments, Mark, BSV_PhanLoai, etc.) for a specific element in Revit inside a Transaction."
)
def revit_set_parameter(element_id: int, parameter_name: str, value: str) -> dict[str, Any]:
    """
    Modify an element parameter in Revit.
    Args:
        element_id: The integer ID of the element to modify.
        parameter_name: Name of the parameter (e.g. 'Comments', 'Mark').
        value: New value to assign.
    """
    payload = {
        "elementId": element_id,
        "parameterName": parameter_name,
        "value": value
    }
    return _request_revit("/api/elements/set-parameter", method="POST", payload=payload)


@server.tool(
    name="revit_get_boq",
    description="Extract Bill of Quantities (BoQ) data from the active Revit model: volume (m3), formwork area (m2), rebar weight (kg) grouped by Mindmap WBS categories (Columns, Beams, Slabs, Walls, Foundations, Stairs, etc.)."
)
def revit_get_boq() -> dict[str, Any]:
    """Extract BoQ quantity takeoff data from active Revit model."""
    return _request_revit("/api/boq/extract", method="POST", payload={})


if __name__ == "__main__":
    server.run()
