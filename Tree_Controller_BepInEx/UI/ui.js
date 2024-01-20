if (typeof yyTreeController != 'object') var yyTreeController = {};

// Function to set yp the selection mode for tree controller tool.
if (typeof yyTreeController.setupYYTCSelectionModeItem !== 'function')
{
    yyTreeController.setupYYTCSelectionModeItem = function() {
        yyTreeController.setupSelectionModeButton("YYTC-single-tree", "single-tree");
        yyTreeController.setupSelectionModeButton("YYTC-building-or-net", "building-or-net");
        yyTreeController.setupSelectionModeButton("YYTC-radius", "radius");
        yyTreeController.setupSelectionModeButton("YYTC-whole-map", "whole-map");

        let selectedMode = document.getElementById(yyTreeController.selectionMode);

        if (selectedMode != null) {
            selectedMode.classList.add("selected");
        }
        yyTreeController.selectionModeItem = document.getElementById("YYTC-selection-mode-item");
        if (yyTreeController.selectionModeItem) {
            yyTreeController.applyLocalization(yyTreeController.selectionModeItem);
        }
    }
}

if (typeof yyTreeController.buildTreeAgeItem !== 'function') {
    yyTreeController.buildTreeAgeItem = function(panelNode, position)  {
        if (document.getElementById("YYTC-tree-age-item") != null) return;
        const changeAgeRowBZY = document.createElement("div");
        changeAgeRowBZY.className = "item_bZY";
        changeAgeRowBZY.id = "YYTC-tree-age-item";
        
        const changeAgeRow = document.createElement("div");
        changeAgeRow.className = "item-content_nNz";
        const changeAgeRowLabel = document.createElement("div");
        changeAgeRowLabel.id = "YYTC-change-age-label";
        changeAgeRowLabel.className = "label_RZX";
        changeAgeRowLabel.innerHTML = "Age";
        const changeAgeButtonsPanel = document.createElement("div");
        changeAgeButtonsPanel.className = "content_ZIz";
        changeAgeButtonsPanel.id = "YYTC-change-age-buttons-panel";

        buildClearAgesButton(changeAgeButtonsPanel);
        buildChangeAgeButton("YYTC-child", "coui://uil/Standard/TreeSapling.svg", changeAgeButtonsPanel);
        buildChangeAgeButton("YYTC-teen", "coui://uil/Standard/TreeTeen.svg", changeAgeButtonsPanel);
        buildChangeAgeButton("YYTC-adult", "coui://uil/Standard/TreeAdult.svg", changeAgeButtonsPanel);
        buildChangeAgeButton("YYTC-elderly", "coui://uil/Standard/TreeElderly.svg", changeAgeButtonsPanel);
        buildChangeAgeButton("YYTC-dead", "coui://uil/Standard/TreeDead.svg", changeAgeButtonsPanel);

        changeAgeRow.appendChild(changeAgeRowLabel);
        changeAgeRow.appendChild(changeAgeButtonsPanel);

        changeAgeRowBZY.appendChild(changeAgeRow);
        panelNode.insertAdjacentElement(position, changeAgeRowBZY);

        let enabled = 0;
        yyTreeController.selectedAges.forEach(function (value, key) {
            if (value == true) {
                document.getElementById(key).classList.add("selected");
                enabled++;
            }
        })
        if (enabled < 5) {
            document.getElementById("YYTC-clear-ages").classList.remove("selected");
        } else if (enabled == 5) {
            document.getElementById("YYTC-clear-ages").classList.add("selected");
        }


        yyTreeController.setTooltip("YYTC-clear-ages", "clear-ages");
        yyTreeController.setTooltip("YYTC-child", "child");
        yyTreeController.setTooltip("YYTC-teen", "teen");
        yyTreeController.setTooltip("YYTC-adult", "adult");
        yyTreeController.setTooltip("YYTC-elderly", "elderly");
        yyTreeController.setTooltip("YYTC-dead", "dead")
    }
}

if (typeof yyTreeController.destroyTreeAgeItem != 'function') {
    yyTreeController.destroyTreeAgeItem = function () {
        let treeAgeItem = document.getElementById("YYTC-tree-age-item");
        if (treeAgeItem != null) {
            treeAgeItem.parentNode.removeChild(treeAgeItem);
        }
    }
}

if (typeof yyTreeController.setupSelectionModeButton !== 'function')
{
    yyTreeController.setupSelectionModeButton = function (id, localeKey)
    {
        const button = document.getElementById(id);
        if (button == null) {
            engine.trigger('TClog', "JS Error: could not setup button " + id);
            return;
        }
        button.onclick = function () {
            engine.trigger('YYTC-ToolModeChanged', this.id);
            const oldSelectedButton = document.getElementById(yyTreeController.selectionMode);
            if (oldSelectedButton != null)
            {
                oldSelectedButton.classList.remove("selected");
            }
            yyTreeController.selectionMode = this.id;
            const thisButton = document.getElementById(this.id);
            thisButton.classList.add("selected");
            if (this.id == "YYTC-radius" && document.getElementById("YYTC-radius-row") == null) {
                yyTreeController.buildRadiusChanger();
            } else if (this.id != "YYTC-radius" && document.getElementById("YYTC-radius-row") != null) {
                yyTreeController.destroyElementByID("YYTC-radius-row");
            }
        }
        yyTreeController.setTooltip(id, localeKey);
    }
}

if (typeof yyTreeController.buildChangeAgeButton !== 'function') {
    buildChangeAgeButton = function(id, src, buttonsPanel) {
        const button = document.createElement("button");
        button.id = id;
        button.className = "button_KVN";
        const img = document.createElement("img");
        img.className = "icon_Ysc";
        img.src = src;
        button.appendChild(img);
        button.onclick = function () {
            const thisButton = document.getElementById(this.id);
            yyTreeController.selectedAges.set(this.id, !yyTreeController.selectedAges.get(this.id));
            const outputArray = [];
            let i = 0;
            yyTreeController.selectedAges.forEach(function (value, key) {
                outputArray[i] = value;
                i++;
            })
            engine.trigger('YYTC-ChangeSelectedAges', outputArray);
            if (yyTreeController.selectedAges.get(this.id) == true) {
                thisButton.classList.add("selected");
            } else {
                thisButton.classList.remove("selected");
            }

            let enabled = 0;
            yyTreeController.selectedAges.forEach(function (value, key) {
                if (value == true) {
                    enabled++;
                }
            })
            if (enabled < 5) {
                document.getElementById("YYTC-clear-ages").classList.remove("selected");
            } else if (enabled == 5) {
                document.getElementById("YYTC-clear-ages").classList.add("selected");
            }
        }
        buttonsPanel.appendChild(button);
    }
}

if (typeof yyTreeController.buildClearAgesButton !== 'function') {
    buildClearAgesButton = function (buttonsPanel) {
        if (document.getElementById("YYTC-clear-ages") != null) return;
        const button = document.createElement("button");
        button.id = "YYTC-clear-ages";
        button.className = "button_KVN";
        const img = document.createElement("img");
        img.className = "icon_Ysc";
        img.src = "Media/Tools/Snap Options/All.svg";
        button.appendChild(img);
        button.onclick = function () {
            let enabled = 0;
            yyTreeController.selectedAges.forEach(function (value, key) {
                if (value == true) {
                    enabled++;
                }
            })
            if (enabled < 5) {
                document.getElementById(this.id).classList.add("selected");
                yyTreeController.selectedAges.forEach(function (value, key) {
                    yyTreeController.selectedAges.set(key, true);
                    document.getElementById(key).classList.add("selected");
                })
            } else if (enabled == 5) {
                document.getElementById(this.id).classList.remove("selected");
                yyTreeController.selectedAges.forEach(function (value, key) {
                    yyTreeController.selectedAges.set(key, false);
                    document.getElementById(key).classList.remove("selected");
                })
            }

            const outputArray = [];
            i = 0;
            yyTreeController.selectedAges.forEach(function (value, key) {
                outputArray[i] = value;
                i++;
            })
            engine.trigger('YYTC-ChangeSelectedAges', outputArray);
        }
        buttonsPanel.appendChild(button);
    }
}

if (typeof yyTreeController.buildRadiusChanger !== 'function') {
    yyTreeController.buildRadiusChanger = function () {
        if (document.getElementById("YYTC-radius-row") != null) return;
        const radiusRow = document.createElement("div");
        radiusRow.id = "YYTC-radius-row";
        radiusRow.className = "item_bZY";

        const radiusItemContent = document.createElement("div");
        radiusItemContent.className = "item-content_nNz";

        const radiusRowLabel = document.createElement("div");
        radiusRowLabel.id = "YYTC-radius-row-label";
        radiusRowLabel.className = "label_RZX";
        radiusRowLabel.localeKey = "YY_TREE_CONTROLLER.Radius";
        const radiusButtonsPanel = document.createElement("div");
        radiusButtonsPanel.className = "content_ZIz";
        radiusButtonsPanel.id = "YYTC-radius-buttons-panel";

        const downButton = document.createElement("button");
        downButton.id = "YYTC-radius-down-arrow";
        downButton.className = "button_KVN";
        const downButtonImg = document.createElement("img");
        downButtonImg.className = "icon_Ysc";
        downButtonImg.src = "coui://uil/Standard/ArrowDownThickStroke.svg";
        downButton.appendChild(downButtonImg);

        const upButton = document.createElement("button");
        upButton.id = "YYTC-radius-up-arrow";
        upButton.className = "button_KVN";
        const upButtonImg = document.createElement("img");
        upButtonImg.className = "icon_Ysc";
        upButtonImg.src = "coui://uil/Standard/ArrowUpThickStroke.svg";
        upButton.appendChild(upButtonImg);

        const radiusField = document.createElement("div");
        radiusField.id = "YYTC-radius-field";
        radiusField.className = "number-field__Hd";
        radiusField.innerHTML = yyTreeController.radius + " m";

        downButton.onclick = function () { yyTreeController.decreaseRadius(); }
        upButton.onclick = function () { yyTreeController.increaseRadius(); }

        radiusButtonsPanel.appendChild(downButton);
        radiusButtonsPanel.appendChild(radiusField);
        radiusButtonsPanel.appendChild(upButton);

        radiusItemContent.appendChild(radiusRowLabel);
        radiusItemContent.appendChild(radiusButtonsPanel);

        radiusRow.appendChild(radiusItemContent);

        const treeControllerToolModeRow = document.getElementById("YYTC-selection-mode-item");
        if (treeControllerToolModeRow != null) {
            treeControllerToolModeRow.insertAdjacentElement("afterend", radiusRow);
        }
        yyTreeController.applyLocalization(document.getElementById("YYTC-radius-row"));
        yyTreeController.setTooltip(upButton.id, "radius-up-arrow");
        yyTreeController.setTooltip(downButton.id, "radius-down-arrow");
    }
}

if (typeof yyTreeController.checkIfNeedToBuildRadius !== 'function') {
    yyTreeController.checkIfNeedToBuildRadius = function () {
        if (yyTreeController.selectionMode == "YYTC-radius") {
            yyTreeController.buildRadiusChanger();
        }
    }
}

// Function to adjust radius.
if (typeof yyTreeController.increaseRadius !== 'function') {
    yyTreeController.increaseRadius = function () {
        if (yyTreeController.radius >= 500 && yyTreeController.radius < 1000) {
            yyTreeController.radius += 100;
        } else if (yyTreeController.radius >= 100 && yyTreeController.radius < 500) {
            yyTreeController.radius += 50;
        } else if (yyTreeController.radius < 1000) {
            yyTreeController.radius += 10;
        }
        engine.trigger('YYTC-AdjustRadius', yyTreeController.radius);
        document.getElementById("YYTC-radius-field").innerHTML = yyTreeController.radius + " m";
    }
}

// Function to adjust radius.
if (typeof yyTreeController.decreaseRadius !== 'function') {
    yyTreeController.decreaseRadius = function () {
        if (yyTreeController.radius <= 100 && yyTreeController.radius > 10) {
            yyTreeController.radius -= 10;
        } else if (yyTreeController.radius <= 500 && yyTreeController.radius > 100) {
            yyTreeController.radius -= 50;
        } else if (yyTreeController.radius > 500) {
            yyTreeController.radius -= 100;
        }
        engine.trigger('YYTC-AdjustRadius', yyTreeController.radius);
        document.getElementById("YYTC-radius-field").innerHTML = yyTreeController.radius + " m";
    }
}
if (typeof yyTreeController.buildActivateToolButton !== 'function') {
    yyTreeController.buildActivateToolButton = function (id, src, buttonsPanel, localeKey) {
        const button = document.createElement("button");
        button.id = id;
        button.className = "button_KVN";
        const img = document.createElement("img");
        img.className = "icon_Ysc";
        img.src = src;
        button.appendChild(img);
        button.onclick = function () {
            engine.trigger(this.id);
        }
        buttonsPanel.appendChild(button);
        yyTreeController.setTooltip(id, localeKey);
    }
}

if (typeof yyTreeController.buildPrefabSetsRow !== 'function') {
    yyTreeController.buildPrefabSetsRow = function (panelNode, position) {
        if (document.getElementById("YYTC-prefab-sets-row") != null) return;
        const prefabSetsRow = document.createElement("div");
        prefabSetsRow.id = "YYTC-prefab-sets-row";
        prefabSetsRow.className = "item_bZY";
        const prefabSetsItemContent = document.createElement("div");
        prefabSetsItemContent.className = "item-content_nNz";

        const prefabSetsRowLabel = document.createElement("div");
        prefabSetsRowLabel.id = "YYTC-prefab-label";
        prefabSetsRowLabel.className = "label_RZX";
        prefabSetsRowLabel.localeKey = "YY_TREE_CONTROLLER.Sets";
        const prefabSetsButtonsPanel = document.createElement("div");
        prefabSetsButtonsPanel.className = "content_ZIz";
        prefabSetsButtonsPanel.id = "YYTC-prefab-sets-buttons-panel";


        yyTreeController.buildPrefabSetButton("YYTC-wild-deciduous-trees", "coui://uil/Standard/TreesDeciduous.svg", prefabSetsButtonsPanel);
        yyTreeController.buildPrefabSetButton("YYTC-evergreen-trees", "coui://uil/Standard/TreesNeedle.svg", prefabSetsButtonsPanel);
        yyTreeController.buildPrefabSetButton("YYTC-wild-bushes", "coui://uil/Standard/Bushes.svg", prefabSetsButtonsPanel);

        prefabSetsItemContent.appendChild(prefabSetsRowLabel);
        prefabSetsItemContent.appendChild(prefabSetsButtonsPanel);

        prefabSetsRow.appendChild(prefabSetsItemContent);

        panelNode.insertAdjacentElement(position, prefabSetsRow);

        const selectedPrefabSetButton = document.getElementById(yyTreeController.selectedPrefabSet);
        if (selectedPrefabSetButton != null) {
            selectedPrefabSetButton.classList.add("selected");
        }
        yyTreeController.applyLocalization(document.getElementById("YYTC-prefab-sets-row"));
        yyTreeController.setTooltip("YYTC-wild-deciduous-trees", "wild-deciduous-trees");
        yyTreeController.setTooltip("YYTC-evergreen-trees", "evergreen-trees");
        yyTreeController.setTooltip("YYTC-wild-bushes", "wild-bushes");
    }
}


// Function to destroy element by ID
if (typeof yyTreeController.destroyElementByID !== 'function') {
    yyTreeController.destroyElementByID = function (id) {
        const element = document.getElementById(id);
        if (element != null) {
            element.parentNode.removeChild(element);
        }
    }
}

if (typeof yyTreeController.buildPrefabSetButton !== 'function') {
    yyTreeController.buildPrefabSetButton = function (id, src, buttonsPanel, localeKey) {
        const button = document.createElement("button");
        button.id = id;
        button.className = "button_KVN";
        const img = document.createElement("img");
        img.className = "icon_Ysc";
        img.src = src;
        button.appendChild(img);
        button.onclick = function () {
            const oldSelectedButton = document.getElementById(yyTreeController.selectedPrefabSet);
            if (oldSelectedButton != null) {
                oldSelectedButton.classList.remove("selected");
            }
            if (yyTreeController.selectedPrefabSet != this.id) {
                yyTreeController.selectedPrefabSet = this.id;
                const thisButton = document.getElementById(this.id);
                thisButton.classList.add("selected");
            } else {
                yyTreeController.selectedPrefabSet = "";
            }
            engine.trigger('YYTC-Prefab-Set-Changed', yyTreeController.selectedPrefabSet);
        }
        buttonsPanel.appendChild(button);
    }
}

if (typeof yyTreeController.buildRotationRow !== 'function') {
    yyTreeController.buildRotationRow = function (panelNode, position) {
        if (document.getElementById("YYTC-rotation-row") != null) return;
        const rotationRow = document.createElement("div");
        rotationRow.id = "YYTC-rotation-row";
        rotationRow.className = "item_bZY";
        const rotationItemContent = document.createElement("div");
        rotationItemContent.className = "item-content_nNz";

        const rotationRowLabel = document.createElement("div");
        rotationRowLabel.id = "YYTC-rotation-label";
        rotationRowLabel.className = "label_RZX";
        rotationRowLabel.localeKey = "YY_TREE_CONTROLLER.Rotation";
        const rotationButtonsPanel = document.createElement("div");
        rotationButtonsPanel.className = "content_ZIz";
        rotationButtonsPanel.id = "YYTC-rotation-buttons-panel";

        const button = document.createElement("button");
        button.id = "YYTC-random-rotation-button";
        button.className = "button_KVN";
        const img = document.createElement("img");
        img.className = "icon_Ysc";
        img.src = "coui://uil/Standard/Dice.svg";
        button.appendChild(img);
        button.onclick = function () {
            yyTreeController.randomRotation = !yyTreeController.randomRotation;
            const thisButton = document.getElementById(this.id);
            if (yyTreeController.randomRotation) {
                thisButton.classList.add("selected");
            } else {
                thisButton.classList.remove("selected");
            }
            engine.trigger('YYTC-Random-Rotation-Changed', yyTreeController.randomRotation);
        }
        rotationButtonsPanel.appendChild(button);

        rotationItemContent.appendChild(rotationRowLabel);
        rotationItemContent.appendChild(rotationButtonsPanel);

        rotationRow.appendChild(rotationItemContent);

        panelNode.insertAdjacentElement(position, rotationRow);

        const selectedPrefabSetButton = document.getElementById("YYTC-random-rotation-button");
        if (selectedPrefabSetButton != null && yyTreeController.randomRotation) {
            selectedPrefabSetButton.classList.add("selected");
        } else if (selectedPrefabSetButton != null && !yyTreeController.randomRotation) {
            selectedPrefabSetButton.classList.remove("selected");
        }

        yyTreeController.setTooltip(button.id, "random-rotation");
        yyTreeController.applyLocalization(document.getElementById("YYTC-rotation-row"));
    }
}

if (typeof yyTreeController.setupToolModeButtons !== 'function') {
    yyTreeController.setupToolModeButtons = function () {
        const ids = ["YYTC-plop-tree", "YYTC-brush-trees", "YYTC-ActivateAgeChange", "YYTC-ActivatePrefabChange"];
        for (let i = 0; i < ids.length; i++) {
            let button = document.getElementById(ids[i]);
            if (button != null) {
                button.onclick = function () {
                    engine.trigger(this.id);
                }
            }
        }
        yyTreeController.toolModeItem = document.getElementById("YYTC-tool-mode-item");
        if (yyTreeController.toolModeItem) {
            yyTreeController.applyLocalization(yyTreeController.toolModeItem);
        }
    }
}

// Function to apply translation strings.
if (typeof yyTreeController.applyLocalization !== 'function') {
    yyTreeController.applyLocalization = function (target) {
        if (!target) {
            return;
        }

        let targets = target.querySelectorAll('[localeKey]');
        targets.forEach(function (currentValue) {
            currentValue.innerHTML = engine.translate(currentValue.getAttribute("localeKey"));
        });
    }
}

// Function to setup tooltip.
if (typeof yyTreeController.setTooltip !== 'function') {
    yyTreeController.setTooltip = function (id, toolTipKey) {
        let target = document.getElementById(id);
        target.onmouseenter = () => yyTreeController.showTooltip(document.getElementById(id), toolTipKey);
        target.onmouseleave = yyTreeController.hideTooltip;
    }
}

// Function to show a tooltip, creating if necessary.
if (typeof yyTreeController.showTooltip !== 'function') {
    yyTreeController.showTooltip = function (parent, tooltipKey) {

        if (!document.getElementById("yytc-tooltip")) {
            yyTreeController.tooltip = document.createElement("div");
            yyTreeController.tooltip.id = "yytc-tooltip";
            yyTreeController.tooltip.style.visibility = "hidden";
            yyTreeController.tooltip.classList.add("balloon_qJY", "balloon_H23", "up_ehW", "center_hug", "anchored-balloon_AYp", "up_el0");
            let boundsDiv = document.createElement("div");
            boundsDiv.classList.add("bounds__AO");
            let containerDiv = document.createElement("div");
            containerDiv.classList.add("container_zgM", "container_jfe");
            let contentDiv = document.createElement("div");
            contentDiv.classList.add("content_A82", "content_JQV");
            let arrowDiv = document.createElement("div");
            arrowDiv.classList.add("arrow_SVb", "arrow_Xfn");
            let broadDiv = document.createElement("div");
            yyTreeController.tooltipTitle = document.createElement("div");
            yyTreeController.tooltipTitle.classList.add("title_lCJ");
            let paraDiv = document.createElement("div");
            paraDiv.classList.add("paragraphs_nbD", "description_dNa");
            yyTreeController.tooltipPara = document.createElement("p");
            yyTreeController.tooltipPara.setAttribute("cohinline", "cohinline");

            paraDiv.appendChild(yyTreeController.tooltipPara);
            broadDiv.appendChild(yyTreeController.tooltipTitle);
            broadDiv.appendChild(paraDiv);
            containerDiv.appendChild(arrowDiv);
            contentDiv.appendChild(broadDiv);
            boundsDiv.appendChild(containerDiv);
            boundsDiv.appendChild(contentDiv);
            yyTreeController.tooltip.appendChild(boundsDiv);

            // Append tooltip to screen element.
            let screenParent = document.getElementsByClassName("game-main-screen_TRK");
            if (screenParent.length == 0) {
                screenParent = document.getElementsByClassName("editor-main-screen_m89");
            }
            if (screenParent.length > 0) {
                screenParent[0].appendChild(yyTreeController.tooltip);
            }
        }

        // Set text and position.
        yyTreeController.tooltipTitle.innerHTML = engine.translate("YY_TREE_CONTROLLER." + tooltipKey);
        yyTreeController.tooltipPara.innerHTML = engine.translate("YY_TREE_CONTROLLER_DESCRIPTION." + tooltipKey);

        // Set visibility tracking to prevent race conditions with popup delay.
        yyTreeController.tooltipVisibility = "visible";

        // Slightly delay popup by three frames to prevent premature activation and to ensure layout is ready.
        window.requestAnimationFrame(() => {
            window.requestAnimationFrame(() => {
                window.requestAnimationFrame(() => {
                    yyTreeController.setTooltipPos(parent);
                });

            });
        });
    }
}

// Function to adjust the position of a tooltip and make visible.
if (typeof yyTreeController.setTooltipPos !== 'function') {
    yyTreeController.setTooltipPos = function (parent) {
        if (!yyTreeController.tooltip) {
            return;
        }

        let tooltipRect = yyTreeController.tooltip.getBoundingClientRect();
        let parentRect = parent.getBoundingClientRect();
        let xPos = parentRect.left + ((parentRect.width - tooltipRect.width) / 2);
        let yPos = parentRect.top - tooltipRect.height;
        yyTreeController.tooltip.setAttribute("style", "left:" + xPos + "px; top: " + yPos + "px; --posY: " + yPos + "px; --posX:" + xPos + "px");

        yyTreeController.tooltip.style.visibility = yyTreeController.tooltipVisibility;
    }
}

// Function to hide the tooltip.
if (typeof yyTreeController.hideTooltip !== 'function') {
    yyTreeController.hideTooltip = function () {
        if (yyTreeController.tooltip) {
            yyTreeController.tooltipVisibility = "hidden";
            yyTreeController.tooltip.style.visibility = "hidden";
        }
    }
}

