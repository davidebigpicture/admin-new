"use strict";

(function (global) {
    function renderNav(container, routes, currentPath) {
        if (!container) {
            return;
        }

        container.innerHTML = "";
        const nav = document.createElement("nav");
        nav.className = "shell-nav";
        nav.setAttribute("aria-label", "Pilot tools");

        (routes || []).forEach(function (route) {
            const link = document.createElement("a");
            link.href = route.path;
            link.textContent = route.label;
            if (route.path === currentPath) {
                link.setAttribute("aria-current", "page");
            }
            nav.appendChild(link);
        });

        container.appendChild(nav);
    }

    function renderSectionMenu(container, sections, currentPath) {
        if (!container) {
            return;
        }
        container.innerHTML = "";

        const toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = "admin-menu-toggle";
        toggle.title = "Collapse admin menu";
        toggle.setAttribute("aria-label", "Collapse admin menu");
        container.appendChild(toggle);

        const content = document.createElement("div");
        content.className = "admin-menu-content";
        container.appendChild(content);

        const filterLabel = document.createElement("label");
        filterLabel.className = "sr-only";
        filterLabel.setAttribute("for", "adminMenuFilter");
        filterLabel.textContent = "Filter admin sections";
        const filter = document.createElement("input");
        filter.id = "adminMenuFilter";
        filter.className = "admin-menu-filter";
        filter.type = "search";
        filter.placeholder = "Find a section or tool";
        content.appendChild(filterLabel);
        content.appendChild(filter);

        const nav = document.createElement("nav");
        nav.className = "admin-section-nav";
        nav.setAttribute("aria-label", "Sections and tools");
        let activeGroup = null;
        (sections || []).forEach(function (section) {
            const group = document.createElement("details");
            group.className = "admin-section";
            group.addEventListener("toggle", function () {
                if (group.open) {
                    closeOtherSections(group);
                }
            });
            const summary = document.createElement("summary");
            summary.textContent = section.Title;
            group.appendChild(summary);

            const list = document.createElement("ul");
            (section.Items || []).forEach(function (item) {
                const listItem = document.createElement("li");
                const link = document.createElement("a");
                link.href = item.Path;
                link.textContent = item.Title || item.Path;
                if (item.Path === currentPath) {
                    link.setAttribute("aria-current", "page");
                    activeGroup = group;
                }
                listItem.appendChild(link);
                list.appendChild(listItem);
            });
            group.appendChild(list);
            nav.appendChild(group);
        });
        content.appendChild(nav);

        function closeOtherSections(currentGroup) {
            Array.prototype.forEach.call(nav.querySelectorAll(".admin-section[open]"), function (group) {
                if (group !== currentGroup) {
                    group.open = false;
                }
            });
        }

        if (activeGroup) {
            closeOtherSections(activeGroup);
            activeGroup.open = true;
        }

        const layout = container.closest(".admin-layout");
        let collapsed = false;
        try {
            collapsed = global.localStorage.getItem("bpAdminMenuCollapsed") === "true";
        } catch (ignore) {
        }
        setCollapsed(collapsed);

        toggle.addEventListener("click", function () {
            setCollapsed(!layout.classList.contains("menu-collapsed"));
        });

        filter.addEventListener("input", function () {
            const query = filter.value.trim().toLowerCase();
            let firstMatch = null;
            Array.prototype.forEach.call(nav.querySelectorAll(".admin-section"), function (group) {
                const matches = !query || group.textContent.toLowerCase().indexOf(query) >= 0;
                group.hidden = !matches;
                if (query && matches && !firstMatch) {
                    firstMatch = group;
                }
            });
            if (firstMatch) {
                closeOtherSections(firstMatch);
                firstMatch.open = true;
            }
        });

        function setCollapsed(value) {
            if (!layout) {
                return;
            }
            layout.classList.toggle("menu-collapsed", value);
            toggle.title = value ? "Show admin menu" : "Collapse admin menu";
            toggle.setAttribute("aria-label", toggle.title);
            toggle.setAttribute("aria-expanded", value ? "false" : "true");
            try {
                global.localStorage.setItem("bpAdminMenuCollapsed", value ? "true" : "false");
            } catch (ignore) {
            }
        }
    }

    function bindLogout(button) {
        if (!button) {
            return;
        }
        button.addEventListener("click", function () {
            const paths = global.PilotSession.paths();
            const logoutUrl = paths.logoutUrl || "../logout.ashx";
            global.location.assign(logoutUrl);
        });
    }

    global.PilotShell = {
        renderNav: renderNav,
        renderSectionMenu: renderSectionMenu,
        bindLogout: bindLogout
    };
}(window));
