# ==============================================================================
# Script de Consultas de Reflexión PowerShell para Modding de Human Host
# ==============================================================================
# Este script contiene las consultas utilizadas para inspeccionar las DLLs
# principales del juego ("UI.dll", "Item_Info.dll", "Assembly-CSharp.dll")
# con el objetivo de descifrar las estructuras de clases clave:
# Loot_Mgr, Icon_Info, Item_Slot_Mgr y Per_Owner_Info.
#
# Instrucciones de uso:
# 1. Asegúrese de que la ruta $ManagedDir sea la correcta en su sistema.
# 2. Ejecute este script en una consola de PowerShell (o cargue sus funciones).
# ==============================================================================

# Ruta por defecto de las librerías del juego
$ManagedDir = "F:\steamtools\steamapps\common\Human Host\Human Host_Data\Managed"

# ------------------------------------------------------------------------------
# 1. Carga de Ensamblados del Juego en el AppDomain de PowerShell
# ------------------------------------------------------------------------------
function Load-GameAssemblies {
    param(
        [string]$Path = $ManagedDir
    )

    if (-not (Test-Path $Path)) {
        Write-Error "La ruta especificada no existe: $Path"
        return
    }

    # DLLs principales a investigar
    $Assemblies = @("UI.dll", "Item_Info.dll", "Assembly-CSharp.dll")

    foreach ($dll in $Assemblies) {
        $fullPath = Join-Path $Path $dll
        if (Test-Path $fullPath) {
            try {
                # Cargar el ensamblado de forma reflexiva
                [System.Reflection.Assembly]::LoadFrom($fullPath) | Out-Null
                Write-Host "[OK] Cargado con éxito: $dll" -ForegroundColor Green
            }
            catch {
                Write-Host "[ERROR] No se pudo cargar $dll : $_" -ForegroundColor Red
            }
        } else {
            Write-Host "[WARNING] Archivo no encontrado: $fullPath" -ForegroundColor Yellow
        }
    }
}

# Cargar automáticamente los ensamblados
Write-Host "Cargando ensamblados del juego..." -ForegroundColor Cyan
Load-GameAssemblies

# ------------------------------------------------------------------------------
# 2. Función para Inspeccionar Tipos (Campos, Propiedades y Métodos)
# ------------------------------------------------------------------------------
function Get-GameTypeDetails {
    param(
        [Parameter(Mandatory=$true)]
        [string]$TypeName
    )

    # Buscar el tipo en todos los ensamblados cargados en el dominio de PowerShell
    $type = $null
    foreach ($assembly in [AppDomain]::CurrentDomain.GetAssemblies()) {
        try {
            $foundType = $assembly.GetType($TypeName)
            if ($null -ne $foundType) {
                $type = $foundType
                Write-Host "Tipo encontrado en: $($assembly.GetName().Name).dll" -ForegroundColor Cyan
                break
            }
        } catch {
            # Omitir excepciones en ensamblados protegidos o dinámicos
        }
    }

    if ($null -eq $type) {
        # Intento de búsqueda parcial por nombre
        Write-Host "No se encontró el tipo '$TypeName'. Buscando tipos similares..." -ForegroundColor Yellow
        Get-GameTypesMatching -Pattern $TypeName
        return
    }

    Write-Host "`n=== DETALLES DEL TIPO: $($type.FullName) ===" -ForegroundColor Green

    # BindingFlags para incluir todo (público, privado, estático, de instancia)
    $flags = [System.Reflection.BindingFlags]::Public -bor 
             [System.Reflection.BindingFlags]::NonPublic -bor 
             [System.Reflection.BindingFlags]::Instance -bor 
             [System.Reflection.BindingFlags]::Static

    # 1. Campos (Fields)
    Write-Host "`n--- CAMPOS (FIELDS) ---" -ForegroundColor Yellow
    $fields = $type.GetFields($flags)
    if ($fields.Count -gt 0) {
        $fields | Select-Object Name, @{Name="FieldType"; Expression={$_.FieldType.Name}}, IsStatic, IsPublic | Format-Table -AutoSize
    } else {
        Write-Host "(Ninguno)" -ForegroundColor Gray
    }

    # 2. Propiedades (Properties)
    Write-Host "`n--- PROPIEDADES (PROPERTIES) ---" -ForegroundColor Yellow
    $properties = $type.GetProperties($flags)
    if ($properties.Count -gt 0) {
        $properties | Select-Object Name, @{Name="PropertyType"; Expression={$_.PropertyType.Name}}, @{Name="CanRead"; Expression={$_.CanRead}}, @{Name="CanWrite"; Expression={$_.CanWrite}} | Format-Table -AutoSize
    } else {
        Write-Host "(Ninguno)" -ForegroundColor Gray
    }

    # 3. Métodos Importantes (Ignorando getters/setters comunes)
    Write-Host "`n--- MÉTODOS CLAVE ---" -ForegroundColor Yellow
    $methods = $type.GetMethods($flags) | Where-Object { 
        -not ($_.Name.StartsWith("get_") -or $_.Name.StartsWith("set_") -or $_.Name.StartsWith("op_"))
    }
    if ($methods.Count -gt 0) {
        $methods | Select-Object Name, @{Name="ReturnType"; Expression={$_.ReturnType.Name}}, IsStatic, IsPublic | Format-Table -AutoSize
    } else {
        Write-Host "(Ninguno)" -ForegroundColor Gray
    }
}

# ------------------------------------------------------------------------------
# 3. Función para buscar tipos por patrón de nombre
# ------------------------------------------------------------------------------
function Get-GameTypesMatching {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Pattern
    )

    Write-Host "Buscando tipos que coincidan con '$Pattern'..." -ForegroundColor Cyan
    
    $results = [System.Collections.Generic.List[PSObject]]::new()
    
    foreach ($assembly in [AppDomain]::CurrentDomain.GetAssemblies()) {
        # Solo inspeccionar ensamblados del juego o de Unity/BepInEx
        if ($assembly.GetName().Name -match "UI|Item_Info|Assembly-CSharp|UnityEngine") {
            try {
                $assembly.GetTypes() | Where-Object { $_.Name -like "*$Pattern*" -or $_.FullName -like "*$Pattern*" } | ForEach-Object {
                    $results.Add([PSCustomObject]@{
                        Assembly = $assembly.GetName().Name + ".dll"
                        TypeName = $_.Name
                        FullName = $_.FullName
                        IsClass  = $_.IsClass
                    })
                }
            } catch {
                # Evitar fallos por tipos no cargables
            }
        }
    }

    if ($results.Count -gt 0) {
        $results | Format-Table -AutoSize
    } else {
        Write-Host "No se encontraron coincidencias." -ForegroundColor Yellow
    }
}

# ==============================================================================
# EJEMPLOS DE CONSULTAS DE REFERENCIA RÁPIDA (Comentar/Descomentar según se requiera)
# ==============================================================================

# A. Buscar información de las clases objetivo del desarrollo:
# -----------------------------------------------------------
# Get-GameTypeDetails -TypeName "Loot_Mgr"
# Get-GameTypeDetails -TypeName "Icon_Info"
# Get-GameTypeDetails -TypeName "Item_Slot_Mgr"
# Get-GameTypeDetails -TypeName "Per_Owner_Info"

# B. Buscar clases relacionadas con palabras clave del juego:
# -----------------------------------------------------------
# Get-GameTypesMatching -Pattern "Loot"
# Get-GameTypesMatching -Pattern "Icon"
# Get-GameTypesMatching -Pattern "Item"
# Get-GameTypesMatching -Pattern "Box"
# Get-GameTypesMatching -Pattern "Storage"
