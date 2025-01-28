Shader "UniBuiltinHWRT/Instance/InstanceAACuboid"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "InstanceHWRayTracing"
            Tags{ "LightMode" = "InstanceHWRayTracing" }

            HLSLPROGRAM

            #pragma raytracing HitShader
            #include "UnityShaderVariables.cginc"
            #include "../Lib/BuiltinHWRTHelperLib.cginc"
            #include "../Lib/PBRHelperLib.cginc"

            struct AABB
            {
                float3 Min;
                float3 Max;
            };

            struct AABBFace
            {
	            float3 center;
	            float3 right;
	            float3 up;
	            float2 extent;
            };

            struct InstanceAACuboidParam
            {
                float4 Color;
                float4 SpecularColor;
                float4 EmissionColor;
                float4 FresnelParameter;
                float EmissionStrength;
                float Smoothness;
                float SpecularProbability;
            };

            struct CuboidAttributeData
            {
                float3 HitPoint;
                float3 Normal;
                int CuboidId;
				float Dis;
            };

            StructuredBuffer<AABB> _InstanceCuboidBuffer;
            StructuredBuffer<InstanceAACuboidParam> _InstanceAACuboidMatParamBuffer;

            bool AABBFaceDetect(AABBFace face, float3 origin, float3 dir, out float dis, out float3 hitPoint, out float3 hitNormal)
			{
				dis = FLT_MAX;
				hitPoint = 0;
				//
				float3 diff = origin - face.center;
				hitNormal = cross(face.right, face.up);
				float DdN = dot(dir, hitNormal);
				bool isHit = false;
				if (DdN != 0)
				{
					float absDdN = abs(DdN);
					float3 DxQ = cross(dir, diff);
					float W1DxQ = dot(face.up, DxQ);
					if (abs(W1DxQ) <= face.extent.x * absDdN)
					{
						float W0DxQ = dot(face.right, DxQ);
						if (abs(W0DxQ) <= face.extent.y * absDdN)
						{
							dis = -dot(diff, hitNormal) * sign(DdN) / max(absDdN, FLT_MIN);
							hitPoint = origin + dis * dir;
							if (dis >= 0 && DdN <= 0)
							{
								isHit = true;
							}
						}
					}
				}
				return isHit;
			}

			bool AABBDetect(float3 Center, float3 Extent, int index, float3 origin, float3 dir, out CuboidAttributeData hitInfo)
			{
				hitInfo = (CuboidAttributeData)0;
				hitInfo.Dis = FLT_MAX;
				hitInfo.CuboidId = index;
				bool isHit = false;
				//
				AABBFace faceFront = (AABBFace)0;
				faceFront.center = Center + float3(0, 0, 1) * Extent.z;
				faceFront.right = float3(1, 0, 0);
				faceFront.up = float3(0, 1, 0);
				faceFront.extent = Extent.xy;
				AABBFace faceBack = (AABBFace)0;
				faceBack.center = Center - float3(0, 0, 1) * Extent.z;
				faceBack.right = -float3(1, 0, 0);
				faceBack.up = float3(0, 1, 0);
				faceBack.extent = Extent.xy;
				AABBFace faceRight = (AABBFace)0;
				faceRight.center = Center + float3(1, 0, 0) * Extent.x;
				faceRight.right = -float3(0, 0, 1);
				faceRight.up = float3(0, 1, 0);
				faceRight.extent = Extent.zy;
				AABBFace faceLeft = (AABBFace)0;
				faceLeft.center = Center - float3(1, 0, 0) * Extent.x;
				faceLeft.right = float3(0, 0, 1);
				faceLeft.up = float3(0, 1, 0);
				faceLeft.extent = Extent.zy;
				AABBFace faceTop = (AABBFace)0;
				faceTop.center = Center + float3(0, 1, 0) * Extent.y;
				faceTop.right = float3(0, 0, 1);
				faceTop.up = float3(1, 0, 0);
				faceTop.extent = Extent.zx;
				AABBFace faceBottom = (AABBFace)0;
				faceBottom.center = Center - float3(0, 1, 0) * Extent.y;
				faceBottom.right = -float3(0, 0, 1);
				faceBottom.up = float3(1, 0, 0);
				faceBottom.extent = Extent.zx;
				//
				float dis;
				float3 hitPoint;
				float3 hitNormal;
				if (AABBFaceDetect(faceFront, origin, dir, dis, hitPoint, hitNormal))
				{
					isHit = true;
					if (hitInfo.Dis > dis)
					{
						hitInfo.Dis = dis;
						hitInfo.HitPoint = hitPoint;
						hitInfo.Normal = hitNormal;
					}
				}
				if (AABBFaceDetect(faceBack, origin, dir, dis, hitPoint, hitNormal))
				{
					isHit = true;
					if (hitInfo.Dis > dis)
					{
						hitInfo.Dis = dis;
						hitInfo.HitPoint = hitPoint;
						hitInfo.Normal = hitNormal;
					}
				}
				if (AABBFaceDetect(faceRight, origin, dir, dis, hitPoint, hitNormal))
				{
					isHit = true;
					if (hitInfo.Dis > dis)
					{
						hitInfo.Dis = dis;
						hitInfo.HitPoint = hitPoint;
						hitInfo.Normal = hitNormal;
					}
				}
				if (AABBFaceDetect(faceLeft, origin, dir, dis, hitPoint, hitNormal))
				{
					isHit = true;
					if (hitInfo.Dis > dis)
					{
						hitInfo.Dis = dis;
						hitInfo.HitPoint = hitPoint;
						hitInfo.Normal = hitNormal;
					}
				}
				if (AABBFaceDetect(faceTop, origin, dir, dis, hitPoint, hitNormal))
				{
					isHit = true;
					if (hitInfo.Dis > dis)
					{
						hitInfo.Dis = dis;
						hitInfo.HitPoint = hitPoint;
						hitInfo.Normal = hitNormal;
					}
				}
				if (AABBFaceDetect(faceBottom, origin, dir, dis, hitPoint, hitNormal))
				{
					isHit = true;
					if (hitInfo.Dis > dis)
					{
						hitInfo.Dis = dis;
						hitInfo.HitPoint = hitPoint;
						hitInfo.Normal = hitNormal;
					}
				}
				return isHit;
			}

            [shader("intersection")]
            void Intersection()
            {
                //ref: https://microsoft.github.io/DirectX-Specs/d3d/Raytracing.html#primitiveindex
                //For D3D12_RAYTRACING_GEOMETRY_TYPE_PROCEDURAL_PRIMITIVE_AABBS, this is the index into the AABB array defining the geometry object.
                int cuboidId = PrimitiveIndex();
				float3 origin = WorldRayOrigin();
                float3 direction = WorldRayDirection();
				//由于我们想要检测的是与当前AABB一样大小的且位置一致的沿轴长方体
                //因此可以将构建RayTracingAccelerationStructure用的graphicsbuffer中的AABB的信息用于ray AABB相交检测
                //为了保险起见
                //ref:https://microsoft.github.io/DirectX-Specs/d3d/Raytracing.html#degenerate-primitives-and-instances
                //
				AABB aabb = _InstanceCuboidBuffer[cuboidId];
				float3 _center = (aabb.Max + aabb.Min) * 0.5;
				float3 _extent = (aabb.Max - aabb.Min) * 0.5;

                CuboidAttributeData cuboidAttributeData;
				if (AABBDetect(_center, _extent, cuboidId, origin, direction, cuboidAttributeData))
				{
					//ref: https://learn.microsoft.com/en-us/windows/win32/api/d3d12/ne-d3d12-d3d12_hit_kind
					ReportHit(cuboidAttributeData.Dis, HIT_KIND_TRIANGLE_FRONT_FACE, cuboidAttributeData);
				}
            }

			[shader("closesthit")]
			void ClosestHit(inout RayPayload rayPayload : SV_RayPayload, CuboidAttributeData attributeData : SV_IntersectionAttributes)
			{
				if (rayPayload.bounceTimes + 1 >= _MaxBounceCount)
				{
					return;
				}
				//
				int cuboidId = attributeData.CuboidId;
				InstanceAACuboidParam matParam = _InstanceAACuboidMatParamBuffer[cuboidId];
				//
				if (matParam.EmissionStrength > 0)
				{
					rayPayload.color = matParam.EmissionStrength * matParam.EmissionColor.xyz;
					return;
				}
				//
				float3 rayDir = WorldRayDirection();
				float3 worldPos = attributeData.HitPoint;//hit pos
				//
				float3 worldNormal = normalize(mul((float3x3)WorldToObject4x3(), attributeData.Normal));
				float3 diffuseDir = normalize(worldNormal + pcgRandomDirection(rayPayload.randomSeed));
				float3 specularDir = reflect(rayDir, worldNormal);
				float3 scatterRayDir = lerp(diffuseDir, specularDir, matParam.Smoothness);
				//
				RayDesc rayDesc;
				rayDesc.Origin = worldPos;
				rayDesc.Direction = scatterRayDir;
				rayDesc.TMin = 0.00001;
				rayDesc.TMax = _ProjectionParams.z;
				//
				RayPayload scatterRayPayload;
				scatterRayPayload.color = 0;
				scatterRayPayload.randomSeed = rayPayload.randomSeed;
				scatterRayPayload.bounceTimes = rayPayload.bounceTimes + 1;
				//
				TraceRay(_SceneAccelStruct, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, 0xFF, 0, 1, 0, rayDesc, scatterRayPayload);//
                float3 fresnel = SchlickFresnelSpecularReflectionOpaque(dot(worldNormal, rayDir), matParam.FresnelParameter.xyz);
                float specularProbability = lerp(matParam.SpecularProbability, 1, fresnel * matParam.Smoothness);
                bool3 isSpecularBounce = specularProbability >= pcgHash(rayPayload.randomSeed);
                rayPayload.color = lerp(matParam.Color.xyz, matParam.SpecularColor.xyz, isSpecularBounce) * scatterRayPayload.color;
			}

            ENDHLSL
        }
    }
}
