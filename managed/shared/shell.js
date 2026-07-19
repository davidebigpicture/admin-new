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
        toggle.setAttribute("aria-controls", "adminMenu");
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

        function closeSection(group) {
            if (!group.open || group.classList.contains("is-closing")) {
                return;
            }
            group.classList.add("is-closing");
            const sectionContent = group.querySelector(".admin-section__content");
            if (sectionContent && "inert" in sectionContent) {
                sectionContent.inert = true;
            }
            global.setTimeout(function () {
                group.open = false;
                group.classList.remove("is-closing");
            }, 180);
        }

        (sections || []).forEach(function (section) {
            const group = document.createElement("details");
            group.className = "admin-section";
            group.addEventListener("toggle", function () {
                if (group.open && !group.classList.contains("is-closing")) {
                    closeOtherSections(group);
                }
            });
            const summary = document.createElement("summary");
            summary.textContent = section.Title;
            summary.addEventListener("click", function (event) {
                event.preventDefault();
                if (group.open) {
                    closeSection(group);
                } else {
                    group.open = true;
                    const sectionContent = group.querySelector(".admin-section__content");
                    if (sectionContent && "inert" in sectionContent) {
                        sectionContent.inert = false;
                    }
                    closeOtherSections(group);
                }
            });
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
            const sectionContent = document.createElement("div");
            sectionContent.className = "admin-section__content";
            sectionContent.appendChild(list);
            group.appendChild(sectionContent);
            nav.appendChild(group);
        });
        content.appendChild(nav);

        function closeOtherSections(currentGroup) {
            Array.prototype.forEach.call(nav.querySelectorAll(".admin-section[open]"), function (group) {
                if (group !== currentGroup) {
                    closeSection(group);
                }
            });
        }

        if (activeGroup) {
            closeOtherSections(activeGroup);
            activeGroup.open = true;
        }

        const layout = container.closest(".admin-layout");
        if (!layout) {
            return;
        }
        const mobileToggle = document.getElementById("adminMenuMobileToggle");
        const menuState = layout._pilotShellMenuState || {
            desktopToggle: null,
            mobileToggle: null,
            content: null,
            mediaQuery: null,
            onViewportChange: null,
            isMobileViewport: null,
            readStoredCollapsed: null,
            setCollapsed: null
        };
        menuState.desktopToggle = toggle;
        menuState.mobileToggle = mobileToggle;
        menuState.content = content;

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

        if (!layout._pilotShellMenuState) {
            menuState.mediaQuery = null;
            menuState.readStoredCollapsed = function () {
                try {
                    return global.localStorage.getItem("bpAdminMenuCollapsed") === "true";
                } catch (ignore) {
                    return false;
                }
            };
            menuState.isMobileViewport = function () {
                if (menuState.mediaQuery) {
                    return menuState.mediaQuery.matches;
                }
                return typeof global.innerWidth === "number" && global.innerWidth <= 900;
            };
            menuState.setCollapsed = function (value, persist) {
                const menuContent = menuState.content;
                if (value && menuContent && menuContent.contains(document.activeElement)) {
                    const controllingToggle = menuState.isMobileViewport()
                        ? menuState.mobileToggle
                        : menuState.desktopToggle;
                    if (controllingToggle && typeof controllingToggle.focus === "function") {
                        controllingToggle.focus();
                    }
                }
                layout.classList.toggle("menu-collapsed", value);
                if (menuContent) {
                    if ("inert" in menuContent) {
                        menuContent.inert = value;
                    } else if (value) {
                        menuContent.setAttribute("inert", "");
                    } else {
                        menuContent.removeAttribute("inert");
                    }
                }
                if (menuState.desktopToggle) {
                    menuState.desktopToggle.title = value ? "Show admin menu" : "Collapse admin menu";
                    menuState.desktopToggle.setAttribute("aria-label", menuState.desktopToggle.title);
                    menuState.desktopToggle.setAttribute("aria-expanded", value ? "false" : "true");
                }
                if (menuState.mobileToggle) {
                    const mobileLabel = value ? "Show admin menu" : "Hide admin menu";
                    menuState.mobileToggle.title = mobileLabel;
                    menuState.mobileToggle.setAttribute("aria-label", mobileLabel);
                    menuState.mobileToggle.setAttribute("aria-expanded", value ? "false" : "true");
                    const mobileIcon = menuState.mobileToggle.querySelector("i");
                    if (mobileIcon) {
                        mobileIcon.classList.toggle("fa-bars", value);
                        mobileIcon.classList.toggle("fa-times", !value);
                    }
                }
                if (persist) {
                    try {
                        global.localStorage.setItem("bpAdminMenuCollapsed", value ? "true" : "false");
                    } catch (ignore) {
                    }
                }
            };
            try {
                if (typeof global.matchMedia === "function") {
                    menuState.mediaQuery = global.matchMedia("(max-width: 900px)");
                }
            } catch (ignore) {
            }
            menuState.onViewportChange = function () {
                menuState.setCollapsed(
                    menuState.isMobileViewport() ? true : menuState.readStoredCollapsed(),
                    false
                );
            };
            if (menuState.mediaQuery) {
                if (typeof menuState.mediaQuery.addEventListener === "function") {
                    menuState.mediaQuery.addEventListener("change", menuState.onViewportChange);
                } else if (typeof menuState.mediaQuery.addListener === "function") {
                    menuState.mediaQuery.addListener(menuState.onViewportChange);
                }
            } else if (typeof global.addEventListener === "function") {
                global.addEventListener("resize", menuState.onViewportChange);
            }
            layout._pilotShellMenuState = menuState;
        }

        toggle.addEventListener("click", function () {
            menuState.setCollapsed(!layout.classList.contains("menu-collapsed"), true);
        });
        if (mobileToggle) {
            mobileToggle.onclick = function () {
                menuState.setCollapsed(!layout.classList.contains("menu-collapsed"), false);
            };
        }
        menuState.setCollapsed(
            menuState.isMobileViewport() ? true : menuState.readStoredCollapsed(),
            false
        );
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
